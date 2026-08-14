using System.Text;

namespace K2PerfMonitor.Web.Services;

/// <summary>สร้าง CSV string จาก list (escape ตาม RFC 4180) — ใช้กับ export ทุกหน้า</summary>
public static class Csv
{
    public static string Build<T>(IEnumerable<T> rows, params (string Header, Func<T, object?> Value)[] columns)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", columns.Select(c => Escape(c.Header))));
        foreach (var row in rows)
            sb.AppendLine(string.Join(",", columns.Select(c => Escape(c.Value(row)?.ToString() ?? ""))));
        return sb.ToString();
    }

    private static string Escape(string field)
    {
        if (field.Contains('"') || field.Contains(',') || field.Contains('\n') || field.Contains('\r'))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
    }
}
