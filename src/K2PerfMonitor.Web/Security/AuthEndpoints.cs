using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace K2PerfMonitor.Web.Security;

/// <summary>
/// Minimal API endpoints สำหรับ cookie sign-in/out
/// (Blazor Server interactive ตั้ง cookie กลาง circuit ไม่ได้ → ใช้ form POST ไป endpoint นี้)
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/account/login", async (HttpContext http, UserService users) =>
        {
            var form = await http.Request.ReadFormAsync();
            var username = form["username"].ToString();
            var password = form["password"].ToString();
            var returnUrl = form["returnUrl"].ToString();

            var user = await users.ValidateAsync(username, password);
            if (user is null)
                return Results.Redirect("/login?error=1" + ReturnParam(returnUrl));

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("uid", user.Id.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            return Results.Redirect(SafeLocal(returnUrl) ?? "/");
        }).DisableAntiforgery(); // form มี anti-CSRF ผ่าน same-site cookie; endpoint นี้ไม่ผ่าน Blazor antiforgery pipeline

        app.MapPost("/account/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/login");
        }).DisableAntiforgery();
    }

    private static string ReturnParam(string? returnUrl)
        => string.IsNullOrEmpty(returnUrl) ? "" : "&returnUrl=" + Uri.EscapeDataString(returnUrl);

    // กัน open-redirect: อนุญาตเฉพาะ path ภายใน
    private static string? SafeLocal(string? url)
        => !string.IsNullOrEmpty(url) && url.StartsWith('/') && !url.StartsWith("//") ? url : null;
}
