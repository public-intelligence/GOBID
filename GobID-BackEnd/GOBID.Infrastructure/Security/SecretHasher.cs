namespace GOBID.Infrastructure.Security;

/// <summary>
/// Comparación de secretos contra hashes BCrypt almacenados.
/// </summary>
public static class SecretHasher
{
    /// <summary>
    /// Compara un valor en claro contra un hash BCrypt. Devuelve false ante cualquier entrada
    /// inutilizable, en vez de propagar la excepción.
    /// </summary>
    /// <remarks>
    /// BCrypt.Verify lanza SaltParseException cuando el hash almacenado no tiene formato BCrypt
    /// —vacío, truncado, o sembrado con otro algoritmo—. Dejar salir esa excepción convierte un
    /// fallo de datos en un fallo aparente del servicio: el llamador responde "Error en
    /// autenticación" en lugar de "Credenciales inválidas", y el intento no queda registrado como
    /// inválido en la auditoría.
    /// </remarks>
    public static bool Verify(string? plainText, string? storedHash)
    {
        if (string.IsNullOrEmpty(plainText) || string.IsNullOrWhiteSpace(storedHash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(plainText, storedHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
