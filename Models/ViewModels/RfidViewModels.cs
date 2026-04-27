using System.ComponentModel.DataAnnotations;
using PaScan.Enums;

namespace PaScan.Models.ViewModels;

// ── M8: Admin RFID Card Issuance ──

public class IssueRfidViewModel
{
    public Guid StudentId { get; set; }
    public string? StudentName { get; set; }

    [Required(ErrorMessage = "Card UID is required.")]
    [StringLength(50, ErrorMessage = "Card UID cannot exceed 50 characters.")]
    public string CardUid { get; set; } = null!;

    [Required(ErrorMessage = "Semester expiry date is required.")]
    [DataType(DataType.Date)]
    public DateTime SemesterExpiresAt { get; set; }
}

public class RfidDetailViewModel
{
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public string StudentNumber { get; set; } = null!;
    public bool IsRfidEnabled { get; set; }
    public int RfidRenewalCount { get; set; }

    // Current active card (null if none)
    public Guid? CardId { get; set; }
    public string? CardUid { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? SemesterExpiresAt { get; set; }
    public TokenStatus? CardStatus { get; set; }
    public int? RenewalNumber { get; set; }
}
