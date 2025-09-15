using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Data;

public class TempApplicationUser
{
    [Key]
    [MaxLength(256)]
    public string ApplicationUserEmail { get; set; }

    [MaxLength(64)]
    public string? EmailConfirmCode { get; set; }

    [MaxLength(256)]
    public string EmailOriginal { get; set; }

    [MaxLength(256)]
    public string? UserNameOriginal { get; set; }
}
