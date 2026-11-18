using System.ComponentModel.DataAnnotations;

namespace RFQAPI.Models.DTOs.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email format is invalid.")]
    [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Company name must be between 2 and 200 characters.")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Registration number is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Registration number is required.")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "Contact person cannot exceed 150 characters.")]
    public string? ContactPerson { get; set; }

    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
    public string? Phone { get; set; }
}