using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PaScan.Enums;

namespace PaScan.Models;

public class RFIDCard : BaseEntity
{
    [Required(ErrorMessage = "Student ID is required.")]
    public Guid StudentId { get; set; }

    [Required(ErrorMessage = "Card UID is required.")]
    [StringLength(50, ErrorMessage = "Card UID cannot exceed 50 characters.")]
    public string CardUid { get; set; } = null!;

    [Required(ErrorMessage = "Issue date is required.")]
    public DateTime IssuedAt { get; set; }

    [Required(ErrorMessage = "Semester expiry date is required.")]
    public DateTime SemesterExpiresAt { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public TokenStatus Status { get; set; }

    [Range(0, 100, ErrorMessage = "Renewal number must be between 0 and 100.")]
    public int RenewalNumber { get; set; }

    public Guid? RenewedFrom { get; set; }
    public bool IsReplacement { get; set; }

    [StringLength(50, ErrorMessage = "Previous card UID cannot exceed 50 characters.")]
    public string? PreviousCardUid { get; set; }

    [Required(ErrorMessage = "Issued by is required.")]
    public Guid IssuedBy { get; set; }

    // Invalidation fields
    public DateTime? InvalidatedAt { get; set; }
    public Guid? InvalidatedBy { get; set; }
    public InvalidationReason? InvalidationReason { get; set; }

    // Navigation
    public Student? Student { get; set; }
    public Admin? AdminIssuer { get; set; }
    public Admin? AdminInvalidator { get; set; }
    public ICollection<GateScanLog> GateScanLogs { get; set; } = new List<GateScanLog>();
}
