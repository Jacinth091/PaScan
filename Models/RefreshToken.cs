using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace PaScan.Models;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = null!;
    public DateTime Expiry { get; set; }
    public bool IsRevoked { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}