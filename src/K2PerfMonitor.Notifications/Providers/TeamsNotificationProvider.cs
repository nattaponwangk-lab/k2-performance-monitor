using System.Text;
using System.Text.Json;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Extensions;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Notifications.Providers;

/// <summary>ส่งเข้า Microsoft Teams ผ่าน Incoming Webhook (MessageCard)</summary>
public sealed class TeamsNotificationProvider : INotificationProvider
{
    private readonly TeamsOptions _opt;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<TeamsNotificationProvider> _logger;

    public TeamsNotificationProvider(
        IOptions<TeamsOptions> opt, IHttpClientFactory httpFactory, ILogger<TeamsNotificationProvider> logger)
    {
        _opt = opt.Value;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public string Name => "Teams";
    public NotificationChannel Channel => NotificationChannel.Teams;

    public bool IsEnabled => _opt.Enabled && !string.IsNullOrWhiteSpace(_opt.WebhookUrl);

    public async Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var facts = new List<object>();
            if (message.CollectorType is not null)
                facts.Add(new { name = "Collector", value = message.CollectorType.ToString() });
            if (!string.IsNullOrWhiteSpace(message.Timestamp))
                facts.Add(new { name = "Time", value = message.Timestamp });

            var section = new Dictionary<string, object?>
            {
                ["activityTitle"] = $"{message.Severity.ToEmoji()} {message.Title}",
                ["text"] = string.IsNullOrWhiteSpace(message.Detail)
                    ? message.Summary
                    : $"{message.Summary}\n\n{message.Detail}",
                ["facts"] = facts,
                ["markdown"] = true
            };

            var card = new Dictionary<string, object?>
            {
                ["@type"] = "MessageCard",
                ["@context"] = "https://schema.org/extensions",
                ["themeColor"] = message.Severity.ToHexColor().TrimStart('#'),
                ["summary"] = message.Title,
                ["sections"] = new[] { section }
            };

            var url = message.DashboardUrl ?? _opt.DashboardUrl;
            if (!string.IsNullOrWhiteSpace(url))
            {
                card["potentialAction"] = new[]
                {
                    new Dictionary<string, object?>
                    {
                        ["@type"] = "OpenUri",
                        ["name"] = "View in Dashboard",
                        ["targets"] = new[] { new { os = "default", uri = url } }
                    }
                };
            }

            var json = JsonSerializer.Serialize(card);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var client = _httpFactory.CreateClient(nameof(TeamsNotificationProvider));
            var resp = await client.PostAsync(_opt.WebhookUrl, content, cancellationToken);

            if (!resp.IsSuccessStatusCode)
                _logger.LogWarning("Teams webhook returned {Status}", (int)resp.StatusCode);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Teams notification failed");
            return false;
        }
    }
}
