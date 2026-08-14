using K2PerfMonitor.Web.Security;
using K2PerfMonitor.Web.Services;

namespace K2PerfMonitor.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_then_verify_succeeds()
    {
        var hash = PasswordHasher.Hash("S3cret!pass");
        Assert.True(PasswordHasher.Verify("S3cret!pass", hash));
    }

    [Fact]
    public void Verify_fails_for_wrong_password()
    {
        var hash = PasswordHasher.Hash("correct-horse");
        Assert.False(PasswordHasher.Verify("wrong-horse", hash));
    }

    [Fact]
    public void Hash_is_salted_different_each_time()
    {
        Assert.NotEqual(PasswordHasher.Hash("same"), PasswordHasher.Hash("same"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-valid-format")]
    [InlineData("1.abc")]
    public void Verify_returns_false_for_malformed_stored(string stored)
        => Assert.False(PasswordHasher.Verify("x", stored));
}

public class CsvInjectionTests
{
    private record Row(string Value);

    [Theory]
    [InlineData("=cmd|'/c calc'!A1")]
    [InlineData("+1+2")]
    [InlineData("-2+3")]
    [InlineData("@SUM(A1)")]
    public void Dangerous_leading_char_is_neutralized(string dangerous)
    {
        var csv = Csv.Build(new[] { new Row(dangerous) }, ("Value", r => r.Value));
        var dataLine = csv.Split('\n')[1];
        // ค่าที่ขึ้นต้นด้วยอักขระอันตรายต้องถูก prefix ด้วย ' (อาจถูกห่อ quote)
        Assert.Contains("'" + dangerous[0], dataLine);
        Assert.False(dataLine.TrimStart('"').StartsWith(dangerous[0]));
    }

    [Fact]
    public void Normal_value_is_unchanged()
    {
        var csv = Csv.Build(new[] { new Row("SELECT 1") }, ("Value", r => r.Value));
        Assert.Contains("SELECT 1", csv);
        Assert.DoesNotContain("'SELECT", csv);
    }

    [Fact]
    public void Comma_and_quote_are_escaped()
    {
        var csv = Csv.Build(new[] { new Row("a,\"b\"") }, ("Value", r => r.Value));
        Assert.Contains("\"a,\"\"b\"\"\"", csv);
    }
}

public class GridStateTests
{
    private record Item(string Name, int Value);

    private static GridState<Item> Make() => new(
        i => i.Name,
        new() { ["name"] = i => i.Name, ["value"] = i => i.Value },
        "value");

    [Fact]
    public void Filter_matches_search_case_insensitive()
    {
        var g = Make();
        g.SetSearch("APP");
        var res = g.Filtered(new[] { new Item("app-1", 1), new Item("db", 2) });
        Assert.Single(res);
        Assert.Equal("app-1", res[0].Name);
    }

    [Fact]
    public void Sort_toggles_direction()
    {
        var g = Make();
        var data = new[] { new Item("a", 1), new Item("b", 3), new Item("c", 2) };
        var desc = g.Filtered(data);              // default value desc
        Assert.Equal(3, desc[0].Value);
        g.ToggleSort("value");                    // -> asc
        Assert.Equal(1, g.Filtered(data)[0].Value);
    }

    [Fact]
    public void Paging_slices_correctly()
    {
        var g = Make() ;
        g.PageSize = 2;
        var data = Enumerable.Range(1, 5).Select(i => new Item($"i{i}", i)).ToList();
        var filtered = g.Filtered(data);
        Assert.Equal(2, g.PageOf(filtered).Count);
        Assert.Equal(3, g.TotalPages(filtered.Count));
    }
}
