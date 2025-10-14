using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Data;

/// <summary>
/// Хранение данных для восстановления паролей.
/// </summary>
/// <remarks>
/// Если привязать дату к учётки, а учётки нету, то у нас уязвимость на "есть ли друг с таким email на нашем сайте".
/// </remarks>
public class RestorePasswordEmail
{
    [Key]
    [MaxLength(256)]
    [ProtectedPersonalData]
    public string Email { get; set; }

    public DateTime? RestorePasswordDate { get; set; }

    [MaxLength(20)]
    [ProtectedPersonalData]
    public string? VerificationCode { get; set; }

    public DateTime? CodeExpiresAt { get; set; }

    public int Attempts { get; set; }
}
