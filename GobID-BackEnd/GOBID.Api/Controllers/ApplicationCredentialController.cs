using GOBID.Api.Controllers.Base;
using GOBID.Application.DTOs.ApplicationCredential;
using GOBID.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GOBID.Api.Controllers;

/// <summary>
/// Gestión de credenciales M2M para aplicaciones (service accounts).
/// Requiere un token de usuario autenticado con permisos de administración.
/// </summary>
[Route("api/v{version:apiVersion}/applications/{applicationId}/credentials")]
public class ApplicationCredentialController : OwnBaseController
{
    private readonly IApplicationCredentialService _credentialService;

    public ApplicationCredentialController(
        IAuthService authService,
        IApplicationCredentialService credentialService) : base(authService)
    {
        _credentialService = credentialService;
    }

    /// <summary>
    /// Lista todas las credenciales de la aplicación (sin secretos).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> List([FromRoute] Guid applicationId)
    {
        var response = await _credentialService.ListByApplicationAsync(applicationId);
        if (!response.Succeeded)
            return BadRequest(response);
        return Ok(response);
    }

    /// <summary>
    /// Crea una nueva credencial M2M para la aplicación.
    /// El client_secret se devuelve UNA SOLA VEZ en esta respuesta. Guárdalo de inmediato.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Create(
        [FromRoute] Guid applicationId,
        [FromBody] CreateAppCredentialDto dto)
    {
        var createdBy = GetUserIdFromToken();
        if (createdBy == null)
            return Unauthorized("No se pudo identificar al usuario autenticado.");

        var response = await _credentialService.CreateAsync(applicationId, dto, createdBy.Value);
        if (!response.Succeeded)
            return BadRequest(response);

        return Created(string.Empty, response);
    }

    /// <summary>
    /// Revoca una credencial. Los tokens activos emitidos con ella siguen válidos hasta expirar,
    /// pero no se podrán renovar ni autenticar nuevas sesiones con esa credencial.
    /// </summary>
    [HttpDelete("{credentialId}")]
    public async Task<ActionResult> Revoke(
        [FromRoute] Guid applicationId,
        [FromRoute] Guid credentialId)
    {
        var revokedBy = GetUserIdFromToken();
        if (revokedBy == null)
            return Unauthorized("No se pudo identificar al usuario autenticado.");

        var response = await _credentialService.RevokeAsync(applicationId, credentialId, revokedBy.Value);
        if (!response.Succeeded)
            return BadRequest(response);

        return Ok(response);
    }
}
