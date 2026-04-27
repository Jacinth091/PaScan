using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PaScan.Models;

public class Admin : BaseEntity
{
    [Required(ErrorMessage = "User ID is required.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
    public string LastName { get; set; } = null!;

    [StringLength(50, ErrorMessage = "Middle name cannot exceed 50 characters.")]
    public string? MiddleName { get; set; }

    [StringLength(100, ErrorMessage = "Position cannot exceed 100 characters.")]
    public string? Position { get; set; }

    [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters.")]
    public string? Department { get; set; }

    public bool IsActive { get; set; } = true;
    public Guid? CreatedBy { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Admin? AdminCreator { get; set; }
    public ICollection<Scanner> ScannersCreated { get; set; } = new List<Scanner>();
}
