using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PaScan.Enums;

namespace PaScan.Models;

public class DeviceRequest : BaseEntity
{
    [Required(ErrorMessage = "Student ID is required.")]
    public Guid StudentId { get; set; }

    [Required(ErrorMessage = "Purpose is required.")]
    [StringLength(500, ErrorMessage = "Purpose cannot exceed 500 characters.")]
    public string Purpose { get; set; } = null!;

    [Required(ErrorMessage = "Device name is required.")]
    [StringLength(100, ErrorMessage = "Device name cannot exceed 100 characters.")]
    public string DeviceName { get; set; } = null!;

    [Required(ErrorMessage = "Device type is required.")]
    public DeviceType DeviceType { get; set; }

    [Required(ErrorMessage = "Brand is required.")]
    [StringLength(50, ErrorMessage = "Brand cannot exceed 50 characters.")]
    public string Brand { get; set; } = null!;

    [Required(ErrorMessage = "Model is required.")]
    [StringLength(50, ErrorMessage = "Model cannot exceed 50 characters.")]
    public string Model { get; set; } = null!;

    [Required(ErrorMessage = "Serial number is required.")]
    [StringLength(100, ErrorMessage = "Serial number cannot exceed 100 characters.")]
    public string SerialNumber { get; set; } = null!;

    // Hardware specs (nullable — not all device types have these)
    [StringLength(100, ErrorMessage = "Operating system cannot exceed 100 characters.")]
    public string? OperatingSystem { get; set; }

    [StringLength(50, ErrorMessage = "Color cannot exceed 50 characters.")]
    public string? Color { get; set; }

    [StringLength(100, ErrorMessage = "Processor cannot exceed 100 characters.")]
    public string? Processor { get; set; }

    [StringLength(100, ErrorMessage = "Motherboard cannot exceed 100 characters.")]
    public string? Motherboard { get; set; }

    [StringLength(100, ErrorMessage = "Memory cannot exceed 100 characters.")]
    public string? Memory { get; set; }

    [StringLength(100, ErrorMessage = "Storage cannot exceed 100 characters.")]
    public string? Storage { get; set; }

    [StringLength(100, ErrorMessage = "Monitor size cannot exceed 100 characters.")]
    public string? MonitorSize { get; set; }

    [StringLength(100, ErrorMessage = "Casing cannot exceed 100 characters.")]
    public string? Casing { get; set; }

    public bool HasCdRom { get; set; }

    // Review fields
    [Required(ErrorMessage = "Status is required.")]
    public RegisterStatus Status { get; set; }

    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    [StringLength(500, ErrorMessage = "Rejection reason cannot exceed 500 characters.")]
    public string? RejectionReason { get; set; }

    [StringLength(500, ErrorMessage = "Remarks cannot exceed 500 characters.")]
    public string? Remarks { get; set; }

    // Navigation
    public Student Student { get; set; } = null!;
    public Admin? AdminReviewer { get; set; }
    public ICollection<DeviceRequestAccessory> Accessories { get; set; } = new List<DeviceRequestAccessory>();
    public Device? ApprovedDevice { get; set; }
}
