using Newtonsoft.Json;

namespace GOBID.Application.DTOs.User;

public class UserDeleteDto
{
    public Guid DeleteBy { get; set; }
    public string? Observation { get; set; }
}