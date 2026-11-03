namespace GOBID.Application.DTOs.Auth;

/// <summary>
/// Respuesta del flujo M2M. Ambos tokens se devuelven en el body (no en cookie)
/// ya que el consumidor es una aplicación server-to-server, no un browser.
/// </summary>
public class AppTokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; } = 3600;
}
