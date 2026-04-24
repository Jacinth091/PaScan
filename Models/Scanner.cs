using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PaScan.Enums;

namespace PaScan.Models;

public class Scanner : BaseEntity
{
    [Required(ErrorMessage = "User ID is required.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Scanner name is required.")]
    [StringLength(100, ErrorMessage = "Scanner name cannot exceed 100 characters.")]
    public string Name { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Location is required.")]
    [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters.")]
    public string Location { get; set; } = null!;

    [Required(ErrorMessage = "Scanner type is required.")]
    public ScannerType ScannerType { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public ScannerStatus Status { get; set; }

    public string? ApiKey { get; set; }
    public DateTime? ApiKeyIssuedAt { get; set; }
    public Guid? ApiKeyGeneratedBy { get; set; }
    public DateTime? InstalledAt { get; set; }

    [Required(ErrorMessage = "Created by is required.")]
    public Guid CreatedBy { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Admin AdminCreator { get; set; } = null!;
    public Admin? AdminKeyGenerator { get; set; }
    public ICollection<GateScanLog> GateScanLogs { get; set; } = new List<GateScanLog>();
}
