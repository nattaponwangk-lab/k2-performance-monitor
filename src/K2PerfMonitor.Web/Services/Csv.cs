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
        // CSV injection: ค่าที่ขึ้นต้นด้วย = + - @ (หรือ tab/CR) อาจถูก spreadsheet ตีความเป็นสูตร
        // → prefix ด้วย single quote เพื่อ neutralize (ข้อมูล query/login มาจาก source ที่ควบคุมไม่ได้)
        if (field.Length > 0 && (field[0] is '=' or '+' or '-' or '@' or '\t' or '\r'))
            field = "'" + field;

        if (field.Contains('"') || field.Contains(',') || field.Contains('\n') || field.Contains('\r'))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
    }
}
