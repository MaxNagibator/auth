using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Data;

/// <summary>
/// Хранение дат отправки кодов для восстановления паролей.
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
}
