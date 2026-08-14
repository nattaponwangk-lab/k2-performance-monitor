using K2PerfMonitor.Notifications;

namespace K2PerfMonitor.Tests;

public class NotificationCooldownTests
{
    private static readonly DateTime Now = new(2026, 7, 4, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NeverNotified_ShouldNotify()
        => Assert.True(NotificationCooldown.ShouldNotify(null, 30, Now));

    [Fact]
    public void ZeroCooldown_AlwaysNotify()
        => Assert.True(NotificationCooldown.ShouldNotify(Now.AddSeconds(-1), 0, Now));

    [Fact]
    public void WithinCooldown_Suppress()
        => Assert.False(NotificationCooldown.ShouldNotify(Now.AddMinutes(-5), 30, Now));

    [Fact]
    public void CooldownElapsed_ShouldNotify()
        => Assert.True(NotificationCooldown.ShouldNotify(Now.AddMinutes(-31), 30, Now));

    [Fact]
    public void ExactlyAtCooldownBoundary_ShouldNotify()
        => Assert.True(NotificationCooldown.ShouldNotify(Now.AddMinutes(-30), 30, Now));
}
