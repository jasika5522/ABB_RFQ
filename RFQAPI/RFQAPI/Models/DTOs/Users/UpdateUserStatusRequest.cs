using System.ComponentModel.DataAnnotations;

namespace RFQAPI.Models.DTOs.Users;

public class UpdateUserStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}