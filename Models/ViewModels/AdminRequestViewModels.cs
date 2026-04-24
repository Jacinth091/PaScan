using PaScan.Enums;

namespace PaScan.Models.ViewModels;

// ── M4: Admin Device Approval ──

public class PendingRequestListItem
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public DeviceType DeviceType { get; set; }
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }
}

public class RequestDetailViewModel
{
    public Guid Id { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public string CourseName { get; set; } = null!;

    // Pink slip fields
    public string Purpose { get; set; } = null!;
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

    public RegisterStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public List<AccessoryViewModel> Accessories { get; set; } = new();
}

public class RejectRequestViewModel
{
    public Guid RequestId { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Rejection reason is required.")]
    public string RejectionReason { get; set; } = null!;
}
