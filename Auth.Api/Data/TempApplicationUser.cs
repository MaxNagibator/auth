using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Data;

public class TempApplicationUser
{
    [Key]
    [PersonalData]
    public string Id { get; set; }

    [MaxLength(256)]
    [ProtectedPersonalData]
    public string Email { get; set; }

    [MaxLength(256)]
    [ProtectedPersonalData]
    public string UserName { get; set; }
    
    [MaxLength(64)]
    [PersonalData]
    public string? EmailConfirmCode { get; set; }
    
    [PersonalData]
    public string ApplicationUserId { get; set; }

    public DateTime? EmailConfirmCodeDate { get; set; }

    public TempApplicationUser()
    {
        Id = Guid.NewGuid().ToString();
    }
}
