using System.Collections.Concurrent;
using K2PerfMonitor.Core.Enums;
using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Core.Options;
using K2PerfMonitor.Core.Results;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Realtime;

/// <summary>
/// IRealtimePublisher ฝั่ง Worker — เชื่อมต่อ SignalR hub ของ Web เป็น client แล้ว relay snapshot/alert
///
/// ออกแบบตาม ROADMAP §14:
/// - throttle: snapshot ต่อ collector type ส่งถี่สุด 1 ครั้ง/วินาที (collapse to latest)
/// - backpressure/resilience: connection ล่ม/hub ไม่ตอบ → กลืน error, ไม่ทำให้ collector ล้ม
///   (realtime เป็น best-effort; ข้อมูลจริงอยู่ใน DB แล้ว)
/// - reconnect อัตโนมัติ (WithAutomaticReconnect)
/// </summary>
public sealed class SignalRRealtimePublisher : IRealtimePublisher, IAsyncDisposable
{
    private readonly SignalROptions _options;
    private readonly ILogger<SignalRRealtimePublisher> _logger;
    private readonly HubConnection? _connection;
    private readonly SemaphoreSlim _startGate = new(1, 1);
    private readonly ConcurrentDictionary<CollectorType, DateTime> _lastSnapshotUtc = new();
    private static readonly TimeSpan MinSnapshotInterval = TimeSpan.FromSeconds(1);
    private volatile bool _startWarned;

    public bool IsEnabled => _options.Enabled && !string.IsNullOrWhiteSpace(_options.HubUrl);

    public SignalRRealtimePublisher(IOptions<SignalROptions> options, ILogger<SignalRRealtimePublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
        if (IsEnabled)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(_options.HubUrl)
                .WithAutomaticReconnect()
                .Build();
        }
    }

    public async Task PublishSnapshotAsync(CollectorResult result, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || _connection is null || result.Items.Count == 0) return;

        // throttle ต่อ collector type
        var now = DateTime.UtcNow;
        if (_lastSnapshotUtc.TryGetValue(result.CollectorType, out var last) && now - last < MinSnapshotInterval)
            return;
        _lastSnapshotUtc[result.CollectorType] = now;

        var metrics = result.Items
            .Where(i => i.MetricField is not null && i.NumericValue.HasValue)
            .GroupBy(i => i.MetricField!)
            .ToDictionary(g => g.Key, g => g.First().NumericValue!.Value);
        if (metrics.Count == 0) return;

        var dto = new MetricSnapshotDto
        {
            CollectorType = result.CollectorType,
            CollectedAtUtc = result.CollectedAtUtc,
            Metrics = metrics
        };
        await SafeInvokeAsync(RealtimeMessages.PublishSnapshot, dto, cancellationToken);
    }

    public async Task PublishAlertAsync(Alert alert, CancellationToken cancellationToken = default)
    {
        if (!IsEnabled || _connection is null) return;
        await SafeInvokeAsync(RealtimeMessages.PublishAlert, AlertDto.From(alert), cancellationToken);
    }

    private async Task SafeInvokeAsync(string method, object arg, CancellationToken ct)
    {
        try
        {
            await EnsureConnectedAsync(ct);
            if (_connection!.State == HubConnectionState.Connected)
                await _connection.InvokeAsync(method, arg, ct);
        }
        catch (Exception ex)
        {
            // best-effort — realtime ล้มไม่กระทบ collection
            _logger.LogDebug(ex, "Realtime publish {Method} failed (ignored)", method);
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken ct)
    {
        if (_connection!.State != HubConnectionState.Disconnected) return;
        await _startGate.WaitAsync(ct);
        try
        {
            if (_connection.State == HubConnectionState.Disconnected)
                await _connection.StartAsync(ct);
        }
        catch (Exception ex)
        {
            if (!_startWarned)
            {
                _logger.LogWarning("Realtime hub not reachable at {Url} — running DB-only until it is up ({Err})",
                    _options.HubUrl, ex.Message);
                _startWarned = true;
            }
        }
        finally { _startGate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}

/// <summary>no-op publisher เมื่อ realtime ปิด (SignalR:Enabled=false)</summary>
public sealed class NullRealtimePublisher : IRealtimePublisher
{
    public bool IsEnabled => false;
    public Task PublishSnapshotAsync(CollectorResult result, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task PublishAlertAsync(Alert alert, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
