using System.ComponentModel.DataAnnotations;

namespace PaScan.Models.ViewModels;

// ── M2: Authentication ──

public class StudentRegisterViewModel
{
    [Required(ErrorMessage = "Firstname is required")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
    public string FirstName {get; set; }= null!;

    [Required(ErrorMessage = "Lastname is required")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
    public string LastName {get; set;} = null!;

    [StringLength(50, ErrorMessage = "Middlename cannot exceed 50 character")]
    public string? MiddleName {get; set;}

    [Required(ErrorMessage = "Student number is required")]
    [StringLength(20, ErrorMessage = "Student number should not exceed 20 characters")]
    [RegularExpression(@"^[a-zA-Z0-9-]+$", ErrorMessage = "Student number can only contain alphanumeric characters and hyphens.")]
    public string StudentNumber{get; set;} = null!;

    [EmailAddress(ErrorMessage = "Invalid  email address")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string? Email{get; set;}

    [Phone(ErrorMessage = "Invalid contact number")]
    [StringLength(15,ErrorMessage = "Contact number cannot exceed 15 characters")]
    public string? ContactNumber{get; set;}
    
    [Range(1, 6, ErrorMessage = "Year level must be between 1 and 6.")]
    public int YearLevel {get; set;}

    [Required(ErrorMessage = "Course is required")]
    public Guid CourseId { get; set; }
    
    [Required(ErrorMessage = "Password is required")]
    public string Password {get; set;} = null!;

    [Required(ErrorMessage = "Password is required")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match ")]
    public string ConfirmPassword {get; set;} = null!;

}

public class StudentLoginViewModel
{
    [Required(ErrorMessage = "Student number is required.")]
    [StringLength(20, ErrorMessage = "Student number cannot exceed 20 characters.")]
    public string StudentNumber { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}

public class AdminLoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}

public class ScannerLoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    public string OldPassword { get; set; } = null!;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters long.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = null!;

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = null!;
}
