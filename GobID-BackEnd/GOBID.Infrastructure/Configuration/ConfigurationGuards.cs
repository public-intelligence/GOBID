using Microsoft.Extensions.Configuration;

namespace GOBID.Infrastructure.Configuration;

public static class ConfigurationGuards
{
    /// <summary>
    /// Devuelve el valor de <paramref name="key"/>, o lanza si está ausente, vacío o sólo espacios.
    /// </summary>
    /// <remarks>
    /// El operador <c>??</c> no sirve para validar secretos: una referencia de Key Vault que no
    /// entrega valor llega como cadena vacía, no como null, así que pasa el guard y el fallo
    /// reaparece mucho después y lejos de su causa — como un 401 del servicio remoto, o como una
    /// clave de firma de longitud cero.
    /// </remarks>
    public static string GetRequired(this IConfiguration configuration, string key)
    {
        var value = configuration[key];

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"La configuración '{key}' está ausente o vacía. Si se resuelve por referencia de " +
                "Key Vault, verificar que la referencia entregue un valor: el estado 'Resolved' no " +
                "garantiza que el worker tenga la versión vigente del secreto.");
        }

        return value;
    }
}
