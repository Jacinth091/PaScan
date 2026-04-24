using System;
using System.ComponentModel.DataAnnotations;

namespace PaScan.Models;

public class DeviceAccessory : BaseEntity
{
    [Required(ErrorMessage = "Device ID is required.")]
    public Guid DeviceId { get; set; }

    [Required(ErrorMessage = "Accessory name is required.")]
    [StringLength(100, ErrorMessage = "Accessory name cannot exceed 100 characters.")]
    public string AccessoryName { get; set; } = null!;

    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
    public int Quantity { get; set; }

    // Navigation
    public Device Device { get; set; } = null!;
}
