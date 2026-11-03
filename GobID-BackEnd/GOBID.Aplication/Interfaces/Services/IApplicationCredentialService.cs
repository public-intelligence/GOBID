using GOBID.Application.DTOs.ApplicationCredential;
using GOBID.Application.Wrappers;

namespace GOBID.Application.Interfaces.Services;

public interface IApplicationCredentialService
{
    /// <summary>
    /// Crea una nueva credencial para la aplicación.
    /// Devuelve el client_secret en claro UNA SOLA VEZ. No se puede recuperar después.
    /// </summary>
    Task<Response<AppCredentialCreatedDto>> CreateAsync(Guid applicationId, CreateAppCredentialDto dto, Guid createdBy);

    /// <summary>
    /// Lista todas las credenciales de la aplicación (sin secretos).
    /// </summary>
    Task<Response<List<AppCredentialDto>>> ListByApplicationAsync(Guid applicationId);

    /// <summary>
    /// Revoca una credencial específica. Valida que la credencial pertenezca a la aplicación indicada.
    /// Las sesiones activas siguen válidas hasta que expiren sus tokens, pero no se pueden renovar.
    /// </summary>
    Task<Response<string>> RevokeAsync(Guid applicationId, Guid appCredentialId, Guid revokedBy);
}
