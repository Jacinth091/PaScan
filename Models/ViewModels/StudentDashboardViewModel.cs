using PaScan.Enums;

namespace PaScan.Models.ViewModels;

// ── M9: Student Dashboard ──

public class StudentDashboardViewModel
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public string CourseName { get; set; } = null!;

    // RFID summary
    public bool IsRfidEnabled { get; set; }
    public DateTime? RfidExpiresAt { get; set; }

    // Device requests
    public List<StudentRequestListItem> Requests { get; set; } = new();

    // Approved devices
    public List<DeviceListItem> Devices { get; set; } = new();
}

public class StudentRequestListItem
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = null!;
    public DeviceType DeviceType { get; set; }
    public RegisterStatus Status { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string? RejectionReason { get; set; }
}
