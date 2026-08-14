using Hangfire.Dashboard;
using K2PerfMonitor.Core.Enums;

namespace K2PerfMonitor.Web.Security;

/// <summary>อนุญาตให้เข้า Hangfire dashboard เฉพาะผู้ใช้ที่ login และเป็น Admin</summary>
public sealed class AdminDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        return http.User.Identity?.IsAuthenticated == true
               && http.User.IsInRole(nameof(UserRole.Admin));
    }
}
