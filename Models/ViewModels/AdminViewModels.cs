using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PaScan.Models;
using PaScan.Enums;

namespace PaScan.Models.ViewModels;

public class AdminRequestListViewModel
{
    public List<PendingRequestListItem> Requests { get; set; } = new();
}

public class AdminRequestDetailViewModel
{
    public RequestDetailViewModel Request { get; set; } = null!;
}

public class AdminStudentListViewModel
{
    public List<Student> Students { get; set; } = new();
}

public class AdminDeviceListViewModel
{
    public List<Device> Devices { get; set; } = new();
}

public class AdminRfidListViewModel
{
    public List<RFIDCard> RfidCards { get; set; } = new();
}

public class AdminDeviceDetailViewModel
{
    public Device Device { get; set; } = null!;
    public QRToken? QrToken { get; set; }
    public string? QrImageBase64 { get; set; }
}

public class AdminStudentDetailViewModel
{
    public Student Student { get; set; } = null!;
    public List<Device> Devices { get; set; } = new();
    public RFIDCard? ActiveRfid { get; set; }
}

public class ScannerListViewModel
{
    public List<ScannerListItem> Scanners { get; set; } = new();
}

public class ScannerCreateViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = null!;

    [Required]
    public ScannerType ScannerType { get; set; }

    [Required]
    public ScannerStatus Status { get; set; }
}

public class ScannerEditViewModel
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; } = null!;

    [Required]
    public ScannerType ScannerType { get; set; }

    [Required]
    public ScannerStatus Status { get; set; }

    public bool RotateApiKey { get; set; }
    public string? CurrentApiKey { get; set; }
}