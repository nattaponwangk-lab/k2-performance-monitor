using System.Net.Http.Headers;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Extensions;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Notifications.Providers;

/// <summary>
/// ส่งเข้า LINE Notify (Bearer token → 1 token = 1 กลุ่ม/ห้อง)
/// หมายเหตุ: LINE Notify กำลังปลดระวาง — อนาคตย้ายไป Messaging API
/// </summary>
public sealed class LineNotificationProvider : INotificationProvider
{
    private readonly LineOptions _opt;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<LineNotificationProvider> _logger;

    public LineNotificationProvider(
        IOptions<LineOptions> opt, IHttpClientFactory httpFactory, ILogger<LineNotificationProvider> logger)
    {
        _opt = opt.Value;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string Name => "LINE";
    public NotificationChannel Channel => NotificationChannel.Line;

    public bool IsEnabled => _opt.Enabled && !string.IsNullOrWhiteSpace(_opt.AccessToken);

    public async Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var text = new System.Text.StringBuilder();
            text.Append($"\n{message.Severity.ToEmoji()} [{message.Severity.ToLabel()}] {message.Title}");
            text.Append($"\n{message.Summary}");
            if (!string.IsNullOrWhiteSpace(message.Detail))
                text.Append($"\n{message.Detail}");
            if (!string.IsNullOrWhiteSpace(message.Timestamp))
                text.Append($"\n🕒 {message.Timestamp}");

            using var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("message", text.ToString())
            });

            using var req = new HttpRequestMessage(HttpMethod.Post, _opt.ApiUrl) { Content = content };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.AccessToken);

            var client = _httpFactory.CreateClient(nameof(LineNotificationProvider));
            var resp = await client.SendAsync(req, cancellationToken);

            if (!resp.IsSuccessStatusCode)
                _logger.LogWarning("LINE Notify returned {Status}", (int)resp.StatusCode);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LINE notification failed");
            return false;
        }
    }
}
