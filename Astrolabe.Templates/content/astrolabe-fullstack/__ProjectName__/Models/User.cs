using System.ComponentModel.DataAnnotations;

namespace AstrolabeApp.Models;

public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string HashedPassword { get; set; } = string.Empty;

    public string? VerificationCode { get; set; }

    public bool EmailVerified { get; set; }

    [MaxLength(20)]
    public string? MfaNumber { get; set; }

    public string? MfaCode { get; set; }

    public DateTime? MfaCodeExpiry { get; set; }

    public string? ResetCode { get; set; }

    public DateTime? ResetCodeExpiry { get; set; }

    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
}
