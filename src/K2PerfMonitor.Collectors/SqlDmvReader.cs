using System.Data;
using Microsoft.Data.SqlClient;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// Helper สำหรับเปิด SqlConnection แบบ read-only และ execute DMV queries อย่างปลอดภัย
///
/// หลักการออกแบบ (ตาม ROADMAP §6 ผลกระทบต่อ source):
/// - lightweight: connect timeout สั้น, command timeout กำหนดได้
/// - ปลอดภัยจาก SQL injection: ใช้ parameter ทุกจุดที่รับค่าจากภายนอก (TopN, threshold ฯลฯ)
/// - อ่านอย่างเดียว: ไม่มี write ใด ๆ ต่อ source DB
/// - เปิด/ปิด connection ต่อ collect หนึ่งรอบ (ไม่ค้าง connection ไว้)
/// </summary>
public sealed class SqlDmvReader : IAsyncDisposable
{
    private readonly SqlConnection _connection;

    /// <summary>command timeout (วินาที) — default 30s กัน DMV query ค้างนาน</summary>
    public int CommandTimeoutSeconds { get; init; } = 30;

    public SqlDmvReader(string connectionString)
    {
        // บังคับ timeout สั้น + ตั้งชื่อ application เพื่อให้ระบุ collector ได้ใน sys.dm_exec_sessions
        var cs = new SqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = "K2PerfMonitor",
            ConnectTimeout = 15
        }.ToString();

        _connection = new SqlConnection(cs);
    }

    /// <summary>เปิด connection (ถ้ายังไม่ได้เปิด)</summary>
    public async Task OpenAsync(CancellationToken ct = default)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(ct);
    }

    /// <summary>Execute query แล้ว map แต่ละ row ด้วย callback (รองรับ parameter แบบ @name)</summary>
    public async Task<List<T>> QueryAsync<T>(
        string sql,
        Func<SqlDataReader, T> map,
        CancellationToken ct = default,
        params (string Name, object? Value)[] parameters)
    {
        var list = new List<T>();
        await using var cmd = CreateCommand(sql, parameters);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            list.Add(map(reader));
        return list;
    }

    /// <summary>Execute scalar query (คืนค่าเดียว) รองรับ parameter</summary>
    public async Task<T?> ExecuteScalarAsync<T>(
        string sql,
        CancellationToken ct = default,
        params (string Name, object? Value)[] parameters)
    {
        await using var cmd = CreateCommand(sql, parameters);
        var result = await cmd.ExecuteScalarAsync(ct);
        if (result is null || result == DBNull.Value)
            return default;

        // แปลงชนิดให้ยืดหยุ่น (เช่น bigint → double)
        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(result, target);
    }

    private SqlCommand CreateCommand(string sql, (string Name, object? Value)[] parameters)
    {
        var cmd = new SqlCommand(sql, _connection) { CommandTimeout = CommandTimeoutSeconds };
        foreach (var (name, value) in parameters)
            cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return cmd;
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

/// <summary>
/// Extension helpers สำหรับอ่านค่าจาก SqlDataReader แบบ null-safe (ใช้ใน collector map)
/// </summary>
public static class SqlDataReaderExtensions
{
    public static string GetStr(this SqlDataReader r, string col)
        => r[col] is string s ? s : r[col]?.ToString() ?? string.Empty;

    public static string? GetStrOrNull(this SqlDataReader r, string col)
        => r[col] == DBNull.Value ? null : r[col]?.ToString();

    public static int GetInt(this SqlDataReader r, string col)
        => r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col]);

    public static long GetLong(this SqlDataReader r, string col)
        => r[col] == DBNull.Value ? 0 : Convert.ToInt64(r[col]);

    public static double GetDouble(this SqlDataReader r, string col)
        => r[col] == DBNull.Value ? 0 : Convert.ToDouble(r[col]);

    public static DateTime? GetDateTimeOrNull(this SqlDataReader r, string col)
        => r[col] == DBNull.Value ? null : Convert.ToDateTime(r[col]);
}
