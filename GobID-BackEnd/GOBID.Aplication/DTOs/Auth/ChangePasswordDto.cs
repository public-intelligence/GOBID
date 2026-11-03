using System.ComponentModel.DataAnnotations;

namespace GOBID.Application.DTOs.Auth;

public class ChangePasswordDto
{
    
    public required string NewPassword { get; set; }
    
    public required string ConfirmNewPassword { get; set; }
    
    public bool? MustChangePassword { get; set; }

}
