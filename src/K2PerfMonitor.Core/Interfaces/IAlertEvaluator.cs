using K2PerfMonitor.Core.Models;
using K2PerfMonitor.Core.Results;

namespace K2PerfMonitor.Core.Interfaces;

/// <summary>
/// ประเมิน collector result เทียบกับ alert rules
/// คืน list ของ alert ที่ควร trigger (พร้อมตรวจ cooldown/dedup)
/// </summary>
public interface IAlertEvaluator
{
    /// <summary>
    /// ประเมินผลลัพธ์ของ collector รอบหนึ่ง เทียบกับ rules ที่เกี่ยวข้อง
    /// </summary>
    Task<IReadOnlyList<Alert>> EvaluateAsync(
        CollectorResult result,
        CancellationToken cancellationToken = default);
}
