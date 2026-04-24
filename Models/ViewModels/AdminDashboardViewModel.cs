using PaScan.Enums;

namespace PaScan.Models.ViewModels;

// ── M10: Admin Dashboard ──

public class AdminDashboardViewModel
{
    // Summary counts
    public int PendingRequestCount { get; set; }
    public int ActiveDeviceCount { get; set; }
    public int TotalStudentCount { get; set; }
    public int TodayScanCount { get; set; }

    // Recent pending requests (top 5)
    public List<PendingRequestListItem> RecentPendingRequests { get; set; } = new();

    // Recent scan logs (top 10)
    public List<ScanLogListItem> RecentScanLogs { get; set; } = new();

    // Active scanners
    public List<ScannerListItem> ActiveScanners { get; set; } = new();
}

public class ScanLogListItem
{
    public Guid Id { get; set; }
    public string? DeviceName { get; set; }
    public string? StudentName { get; set; }
    public ScanType ScanType { get; set; }
    public string ScannerName { get; set; } = null!;
    public bool IsAllowed { get; set; }
    public string? DenialReason { get; set; }
    public DateTime ScannedAt { get; set; }
}

public class ScannerListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Location { get; set; } = null!;
    public ScannerType ScannerType { get; set; }
    public ScannerStatus Status { get; set; }
}
