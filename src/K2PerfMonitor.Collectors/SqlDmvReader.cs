using Microsoft.Data.SqlClient;

namespace K2PerfMonitor.Collectors;

/// <summary>
/// Helper สำหรับเปิด SqlConnection แบบ read-only และ execute DMV queries
/// ใช้ Microsoft.Data.SqlClient (ติดตั้งใน Worker project แล้ว)
/// </summary>
public sealed class SqlDmvReader : IAsyncDisposable
{
    private readonly SqlConnection _connection;

    public SqlDmvReader(string connectionString)
    {
        // บังคับ read-only intent + timeout สั้น (เพื่อไม่ให้ collector ค้าง)
        var cs = new SqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = "K2PerfMonitor",
            ConnectTimeout = 15,
            // ApplicationIntent = ApplicationIntent.ReadOnly  // ใช้กับ AlwaysOn listener เท่านั้น
        }.ToString();

        _connection = new SqlConnection(cs);
    }

    /// <summary>เปิด connection (ถ้ายังไม่ได้เปิด)</summary>
    public async Task OpenAsync(CancellationToken ct = default)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
            await _connection.OpenAsync(ct);
    }

    /// <summary>Execute query แล้ว map แต่ละ row ด้วย callback</summary>
    public async Task<List<T>> QueryAsync<T>(string sql, Func<SqlDataReader, T> map, CancellationToken ct = default)
    {
        var list = new List<T>();
        await using var cmd = new SqlCommand(sql, _connection) { CommandTimeout = 30 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(map(reader));
        }
        return list;
    }

    /// <summary>Execute scalar query (คืนค่าเดียว)</summary>
    public async Task<T?> ExecuteScalarAsync<T>(string sql, CancellationToken ct = default)
    {
        await using var cmd = new SqlCommand(sql, _connection) { CommandTimeout = 30 };
        var result = await cmd.ExecuteScalarAsync(ct);
        return result == null || result == DBNull.Value ? default : (T)result;
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}
