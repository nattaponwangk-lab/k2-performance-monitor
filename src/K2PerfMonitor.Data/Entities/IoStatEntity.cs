using System.ComponentModel.DataAnnotations.Schema;

namespace K2PerfMonitor.Data.Entities;

/// <summary>
/// I/O stats — จาก sys.dm_io_virtual_file_stats
/// SourceKey = "{DatabaseName}|{FileLogicalName}"
/// </summary>
public class IoStatEntity : MetricEntityBase
{
    [MaxLength(128)] public string DatabaseName { get; set; } = string.Empty;
    [MaxLength(128)] public string? LogicalFileName { get; set; }
    [MaxLength(16)] public string? FileType { get; set; }   // ROWS / LOG

    /// <summary>จำนวน bytes อ่าน/เขียน (delta)</summary>
    public long NumOfReads { get; set; }
    public long NumOfWrites { get; set; }
    public long BytesRead { get; set; }
    public long BytesWritten { get; set; }

    /// <summary>I/O stall (ms) — เวลารอ I/O (delta)</summary>
    public double IoStallReadMs { get; set; }
    public double IoStallWriteMs { get; set; }

    /// <summary>stall ต่อ operation (ms)</summary>
    public double IoStallMsPerRead { get; set; }
    public double IoStallMsPerWrite { get; set; }
}
