using System;
using System.Collections.Generic;
using System.Linq;
using GOBID.Application.DTOs.User;

namespace GOBID.Infrastructure.Services;

/// <summary>
/// Selección de un usuario por correo exacto. read_user_accounts filtra con email_like (coincidencia
/// parcial, útil para buscar); una consulta de un solo usuario por correo debe quedarse solo con el
/// correo idéntico al pedido (sin distinguir mayúsculas ni espacios), o con ninguno.
/// </summary>
public static class UserEmailMatch
{
    public static UserDto? PickExact(IEnumerable<UserDto>? candidates, string? email)
    {
        if (candidates is null || string.IsNullOrWhiteSpace(email))
            return null;

        var wanted = email.Trim();
        return candidates.FirstOrDefault(u =>
            u?.Email is not null && string.Equals(u.Email.Trim(), wanted, StringComparison.OrdinalIgnoreCase));
    }
}
