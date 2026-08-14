namespace K2PerfMonitor.Web.Services;

public enum QueryStatus { Ok, Empty, Error }

/// <summary>
/// ผลการ query ที่ห่อสถานะไว้ → หน้า UI แสดง Loading/Empty/Error ได้ครบทุก state
/// (ไม่ต้องเดาว่า list ว่าง = ไม่มีข้อมูล หรือ = error)
/// </summary>
public sealed class QueryResult<T>
{
    public QueryStatus Status { get; init; }
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public DateTime? AsOfUtc { get; init; }
    public string? ErrorMessage { get; init; }

    public bool IsOk => Status == QueryStatus.Ok;
    public bool IsEmpty => Status == QueryStatus.Empty;
    public bool IsError => Status == QueryStatus.Error;

    public static QueryResult<T> Ok(IReadOnlyList<T> items, DateTime? asOf)
        => new() { Status = QueryStatus.Ok, Items = items, AsOfUtc = asOf };
    public static QueryResult<T> Empty()
        => new() { Status = QueryStatus.Empty };
    public static QueryResult<T> Error(string msg)
        => new() { Status = QueryStatus.Error, ErrorMessage = msg };
}
