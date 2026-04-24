using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PaScan.Enums;

namespace PaScan.Models;

public class QRToken : BaseEntity
{
    [Required(ErrorMessage = "Device ID is required.")]
    public Guid DeviceId { get; set; }

    [Required(ErrorMessage = "Student ID is required.")]
    public Guid StudentId { get; set; }

    [Required(ErrorMessage = "Token value is required.")]
    public string TokenValue { get; set; } = null!;

    [Required(ErrorMessage = "Issue date is required.")]
    public DateTime IssuedAt { get; set; }

    [Required(ErrorMessage = "Expiry date is required.")]
    public DateTime ExpiresAt { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    public TokenStatus Status { get; set; }

    [Range(0, 100, ErrorMessage = "Renewal number must be between 0 and 100.")]
    public int RenewalNumber { get; set; }

    public Guid? RenewedFrom { get; set; }

    // Revocation fields (filled only when status = REVOKED)
    public DateTime? RevokedAt { get; set; }
    public Guid? RevokedBy { get; set; }
    public RevocationReason? RevocationReason { get; set; }

    // Navigation
    public Device Device { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public Admin? AdminRevoker { get; set; }
    public ICollection<GateScanLog> GateScanLogs { get; set; } = new List<GateScanLog>();
}
