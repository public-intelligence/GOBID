namespace GOBID.Application.DTOs.Auth;

// El refresh token se recibe via cookie httpOnly, no por el body del request.
// Este DTO se mantiene por compatibilidad pero no se usa en los endpoints.
public class RefreshTokenDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
