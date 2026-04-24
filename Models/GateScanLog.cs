using System;
using PaScan.Enums;

namespace PaScan.Models;

public class GateScanLog : BaseEntity
{
    public Guid DeviceId { get; set; }
    public Guid? QrTokenId { get; set; }
    public Guid? RfidCardId { get; set; }
    public ScanType ScanType { get; set; }
    public Guid ScannerId { get; set; }
    public DateTime ScannedAt { get; set; }
    public bool IsAllowed { get; set; }
    public string? DenialReason { get; set; }

    // Navigation
    public Device Device { get; set; } = null!;
    public QRToken? QrToken { get; set; }
    public RFIDCard? RfidCard { get; set; }
    public Scanner Scanner { get; set; } = null!;
}
