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
    public Device Device { get; set; } = null!;
    public QRToken? QrToken { get; set; }
    public string? QrImageBase64 { get; set; }  // null if token is REVOKED
}
