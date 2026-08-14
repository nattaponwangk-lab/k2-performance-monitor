namespace K2PerfMonitor.Web.Services;

/// <summary>
/// สถานะตารางฝั่ง client: filter (ข้อความ) + sort (คลิกหัวคอลัมน์) + paging
/// ใช้ร่วมกันทุกหน้า metric เพื่อไม่ให้เขียนซ้ำ (ชุดข้อมูลต่อรอบ = TopN ~20-50 แถว → client-side พอเพียง)
/// </summary>
public sealed class GridState<T>
{
    public int PageSize { get; set; } = 25;
    public int Page { get; private set; } = 1;
    public string Search { get; private set; } = "";
    public string? SortKey { get; private set; }
    public bool SortDesc { get; private set; } = true;

    private readonly Func<T, string> _searchText;
    private readonly Dictionary<string, Func<T, IComparable>> _sorters;
    private readonly string _defaultSort;

    public GridState(Func<T, string> searchText, Dictionary<string, Func<T, IComparable>> sorters, string defaultSort)
    {
        _searchText = searchText;
        _sorters = sorters;
        _defaultSort = defaultSort;
        SortKey = defaultSort;
    }

    public void SetSearch(string? s) { Search = s ?? ""; Page = 1; }

    public void ToggleSort(string key)
    {
        if (SortKey == key) SortDesc = !SortDesc;
        else { SortKey = key; SortDesc = true; }
        Page = 1;
    }

    public string SortIndicator(string key) => SortKey == key ? (SortDesc ? " ▼" : " ▲") : "";

    public void NextPage(int total) { if (Page * PageSize < total) Page++; }
    public void PrevPage() { if (Page > 1) Page--; }

    /// <summary>รายการที่ผ่าน filter+sort ทั้งหมด (สำหรับ CSV export = เคารพ filter)</summary>
    public List<T> Filtered(IEnumerable<T> source)
    {
        IEnumerable<T> q = source;
        if (!string.IsNullOrWhiteSpace(Search))
        {
            var s = Search.Trim();
            q = q.Where(x => _searchText(x).Contains(s, StringComparison.OrdinalIgnoreCase));
        }
        var key = SortKey ?? _defaultSort;
        if (_sorters.TryGetValue(key, out var sorter))
            q = SortDesc ? q.OrderByDescending(sorter) : q.OrderBy(sorter);
        return q.ToList();
    }

    /// <summary>slice ของหน้าปัจจุบัน</summary>
    public List<T> PageOf(List<T> filtered)
    {
        var maxPage = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
        if (Page > maxPage) Page = maxPage;
        return filtered.Skip((Page - 1) * PageSize).Take(PageSize).ToList();
    }

    public int TotalPages(int total) => Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
}
