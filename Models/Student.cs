using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PaScan.Models;

public class Student : BaseEntity
{
    [Required(ErrorMessage = "User ID is required.")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Course ID is required.")]
    public Guid CourseId { get; set; }

    [Required(ErrorMessage = "Student number is required.")]
    [StringLength(20, ErrorMessage = "Student number cannot exceed 20 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9-]+$", ErrorMessage = "Student number can only contain alphanumeric characters and hyphens.")]
    public string StudentNumber { get; set; } = null!;

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
    public string LastName { get; set; } = null!;

    [StringLength(50, ErrorMessage = "Middle name cannot exceed 50 characters.")]
    public string? MiddleName { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Invalid contact number.")]
    [StringLength(15, ErrorMessage = "Contact number cannot exceed 15 characters.")]
    public string? ContactNumber { get; set; }

    [Range(1, 6, ErrorMessage = "Year level must be between 1 and 6.")]
    public int YearLevel { get; set; }

    public bool IsRfidEnabled { get; set; }
    public int RfidRenewalCount { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public User User { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public ICollection<DeviceRequest> DeviceRequests { get; set; } = new List<DeviceRequest>();
    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<RFIDCard> RFIDCards { get; set; } = new List<RFIDCard>();
}
