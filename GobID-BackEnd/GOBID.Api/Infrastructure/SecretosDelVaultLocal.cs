using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

namespace GOBID.Api.Infrastructure
{
    /// <summary>
    /// Traduce los nombres de los secretos del vault a las claves de configuracion que la API lee.
    /// </summary>
    /// <remarks>
    /// Solo aplica al desarrollo local (ver Program.cs). Este sistema comparte el vault de desarrollo
    /// con los demas, asi que sus secretos van prefijados con el nombre del sistema. El Load() de
    /// abajo solo acepta los de este mapa: la aplicacion no carga ni ve los secretos de los otros
    /// sistemas, aunque el vault los contenga.
    ///
    /// La traduccion hace falta porque Key Vault no admite ':' en los nombres: las claves de
    /// secrets.example.json (Jwt:Key, connectionStrings:default...) no pueden existir tal cual en
    /// el vault. Alli van en kebab-case con prefijo, y aqui se devuelven a la clave exacta que lee
    /// el codigo.
    ///
    /// El mapa es explicito para que un secreto nuevo con un nombre que la API no conoce quede
    /// fuera en lugar de colarse con una clave que nadie lee. Al agregar un secreto a
    /// secrets.example.json, agregarlo tambien aqui.
    /// </remarks>
    public sealed class SecretosDelVaultLocal : KeyVaultSecretManager
    {
        private static readonly Dictionary<string, string> Equivalencias =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["sql-defaultconnection"] = "connectionStrings:default",
                ["jwt-key"] = "Jwt:Key",
                ["jwt-appkey"] = "Jwt:AppKey",
                ["exceptionless-apikey"] = "Exceptionless:ApiKey",
                ["graphmail-clientsecret"] = "GraphMailSettings:ClientSecret",
                ["pds-m2mclientsecret"] = "PdsApiSettings:M2MClientSecret",
                ["dms-clientsecret"] = "DmsApiSettings:clientSecret",
            };

        public override bool Load(SecretProperties properties) =>
            Equivalencias.ContainsKey(properties.Name);

        public override string GetKey(KeyVaultSecret secret) =>
            Equivalencias.TryGetValue(secret.Name, out var clave) ? clave : base.GetKey(secret);
    }
}
