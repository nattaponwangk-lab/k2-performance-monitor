using System.Net;
using System.Net.Mail;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Extensions;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Notifications.Providers;

/// <summary>ส่งอีเมลผ่าน SMTP (System.Net.Mail)</summary>
public sealed class EmailNotificationProvider : INotificationProvider
{
    private readonly EmailOptions _opt;
    private readonly ILogger<EmailNotificationProvider> _logger;

    public EmailNotificationProvider(IOptions<EmailOptions> opt, ILogger<EmailNotificationProvider> logger)
    {
        _opt = opt.Value;
        _logger = logger;
    }

    public string Name => "Email";
    public NotificationChannel Channel => NotificationChannel.Email;

    public bool IsEnabled => _opt.Enabled
        && !string.IsNullOrWhiteSpace(_opt.Host)
        && !string.IsNullOrWhiteSpace(_opt.FromAddress)
        && !string.IsNullOrWhiteSpace(_opt.ToAddresses);

    public async Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            using var mail = new MailMessage
            {
                From = new MailAddress(_opt.FromAddress, _opt.FromName),
                Subject = $"[{message.Severity.ToLabel()}] {message.Title}",
                IsBodyHtml = true,
                Body = BuildHtml(message)
            };

            foreach (var to in _opt.ToAddresses.Split(
                new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                mail.To.Add(to);
            }

            using var client = new SmtpClient(_opt.Host, _opt.Port) { EnableSsl = _opt.UseSsl };
            if (!string.IsNullOrEmpty(_opt.UserName))
                client.Credentials = new NetworkCredential(_opt.UserName, _opt.Password);

            await client.SendMailAsync(mail, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email notification failed");
            return false;
        }
    }

    private static string BuildHtml(NotificationMessage m)
    {
        var color = m.Severity.ToHexColor();
        return $"""
            <div style="font-family:Segoe UI,Arial,sans-serif;max-width:640px">
              <div style="border-left:6px solid {color};padding:12px 16px;background:#f7f7f7">
                <h2 style="margin:0 0 8px;color:{color}">{m.Severity.ToEmoji()} {WebUtility.HtmlEncode(m.Title)}</h2>
                <p style="margin:0 0 8px">{WebUtility.HtmlEncode(m.Summary)}</p>
                {(string.IsNullOrWhiteSpace(m.Detail) ? "" : $"<pre style='white-space:pre-wrap'>{WebUtility.HtmlEncode(m.Detail)}</pre>")}
                <p style="margin:8px 0 0;color:#666;font-size:12px">
                  {(m.CollectorType is null ? "" : $"Collector: {m.CollectorType} · ")}{m.Timestamp}
                </p>
              </div>
            </div>
            """;
    }
}
