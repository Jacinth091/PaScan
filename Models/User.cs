using System;
using System.ComponentModel.DataAnnotations;
using PaScan.Enums;

namespace PaScan.Models;

public class User : BaseEntity
{
    [StringLength(20, ErrorMessage = "Student number cannot exceed 20 characters.")]
    public string? StudentNumber { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Password hash is required.")]
    public string PasswordHash { get; set; } = null!;

    [Required(ErrorMessage = "Role is required.")]
    public Role Role { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public Student? Student { get; set; }
    public Admin? Admin { get; set; }
    public Scanner? Scanner { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
