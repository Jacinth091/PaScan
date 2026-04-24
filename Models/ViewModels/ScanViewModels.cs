using PaScan.Enums;

namespace PaScan.Models.ViewModels;

// ── M6 & M7: Gate Scanning ──

public class QrScanResultViewModel
{
    public bool Allowed { get; set; }
    public string? DenialReason { get; set; }
    public string? DeviceName { get; set; }
    public string? StudentName { get; set; }
}

public class RfidScanRequest
{
    public string CardUid { get; set; } = null!;
}

public class RfidScanResultViewModel
{
    public bool Allowed { get; set; }
    public string? DenialReason { get; set; }
    public string? StudentName { get; set; }
    public int DeviceCount { get; set; }
}
