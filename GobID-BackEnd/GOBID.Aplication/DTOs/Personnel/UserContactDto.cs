namespace GOBID.Application.DTOs.Personnel;

// Identidad + correo de un usuario GOBID, usado para que sistemas consumidores (ej. SGSC)
// puedan resolver notificaciones ligadas a la identidad real de GOBID, no solo al correo.
public class UserContactDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}
