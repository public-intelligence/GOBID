namespace GOBID.Application.DTOs.Auth;

/// <summary>
/// Payload para autenticación M2M (Client Credentials Grant).
/// El cliente envía su client_id + client_secret y obtiene tokens directamente.
/// </summary>
public class AppLoginRequestDto
{
    /// <summary>ApplicationId de la aplicación que se autentica.</summary>
    public required Guid ClientId { get; set; }

    /// <summary>Secret en claro (se valida contra el BCrypt hash almacenado).</summary>
    public required string ClientSecret { get; set; }
}
