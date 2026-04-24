using PaScan.Enums;

namespace PaScan.Models.ViewModels;

// ── M5: QR Token Display ──

public class DeviceListItem
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = null!;
    public DeviceType DeviceType { get; set; }
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public DeviceStatus Status { get; set; }
    public DateTime? QrExpiresAt { get; set; }
    public TokenStatus? QrStatus { get; set; }
}

public class DeviceDetailViewModel
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = null!;
    public DeviceType DeviceType { get; set; }
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string SerialNumber { get; set; } = null!;
    public string? OperatingSystem { get; set; }
    public string? Color { get; set; }
    public string? Processor { get; set; }
    public string? Motherboard { get; set; }
    public string? Memory { get; set; }
    public string? Storage { get; set; }
    public string? MonitorSize { get; set; }
    public string? Casing { get; set; }
    public bool HasCdRom { get; set; }
    public DeviceStatus Status { get; set; }
    public DateTime ApprovedAt { get; set; }

    // QR info
    public Guid? QrTokenId { get; set; }
    public string? QrTokenValue { get; set; }
    public TokenStatus? QrStatus { get; set; }
    public DateTime? QrIssuedAt { get; set; }
    public DateTime? QrExpiresAt { get; set; }
    public int? QrDaysRemaining { get; set; }
    public int QrRenewalCount { get; set; }

    // Accessories
    public List<AccessoryViewModel> Accessories { get; set; } = new();
}
