using K2PerfMonitor.Core.Interfaces;
using K2PerfMonitor.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace K2PerfMonitor.Realtime;

public static class RealtimeServiceCollectionExtensions
{
    /// <summary>
    /// ลงทะเบียน IRealtimePublisher ฝั่ง Worker — เลือก SignalR client หรือ Null ตาม SignalR:Enabled
    /// </summary>
    public static IServiceCollection AddRealtimePublisher(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<SignalROptions>()
            .Bind(config.GetSection(SignalROptions.SectionName));

        services.AddSingleton<IRealtimePublisher>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<SignalROptions>>();
            if (!opts.Value.Enabled || string.IsNullOrWhiteSpace(opts.Value.HubUrl))
                return new NullRealtimePublisher();
            return new SignalRRealtimePublisher(opts,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<SignalRRealtimePublisher>>());
        });
        return services;
    }
}
