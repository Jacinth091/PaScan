using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PaScan.Models;

public class Course : BaseEntity
{
    [Required(ErrorMessage = "Course code is required.")]
    [StringLength(20, ErrorMessage = "Course code cannot exceed 20 characters.")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Course name is required.")]
    [StringLength(200, ErrorMessage = "Course name cannot exceed 200 characters.")]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Student> Students { get; set; } = new List<Student>();
}
