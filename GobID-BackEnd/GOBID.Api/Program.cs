using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using GOBID.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace GOBID.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((contexto, config) =>
                {
                    // ── Key Vault para desarrollo local ───────────────────────────────────
                    // Para que nadie tenga que guardar credenciales en su maquina: los secretos de
                    // trabajo local salen del vault de desarrollo. DefaultAzureCredential usa la
                    // sesion de `az login` (o la cuenta de Visual Studio).
                    //
                    // Es opcional: sin `KeyVault:LocalUri` no pasa nada y todo sigue como antes.
                    //
                    // En los ambientes desplegados esto NO corre: alli los secretos llegan como
                    // App Settings con referencias al vault (ver docs/secrets-rules.md).
                    var uriDelVaultLocal = config.Build()["KeyVault:LocalUri"];
                    if (!contexto.HostingEnvironment.IsDevelopment() || string.IsNullOrWhiteSpace(uriDelVaultLocal))
                        return;

                    // AddAzureKeyVault no lee el vault al llamarlo sino al construir la configuracion,
                    // asi que se construye aqui mismo: si no, un fallo de credenciales saltaria fuera
                    // de este try sin el mensaje que dice como resolverlo.
                    IConfigurationRoot secretosDelVault;
                    try
                    {
                        secretosDelVault = new ConfigurationBuilder()
                            .AddAzureKeyVault(
                                new Uri(uriDelVaultLocal),
                                new DefaultAzureCredential(),
                                new AzureKeyVaultConfigurationOptions { Manager = new SecretosDelVaultLocal() })
                            .Build();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"No se pudo leer el Key Vault de desarrollo ({uriDelVaultLocal}). Revisa que " +
                            "tengas sesion abierta con `az login` en el tenant del vault y el rol " +
                            "'Key Vault Secrets User' sobre ese vault. Si no lo necesitas, quita la clave " +
                            "'KeyVault:LocalUri' de tus user-secrets.", ex);
                    }
                    config.AddConfiguration(secretosDelVault);

                    // El vault se agrega al final, asi que ganaria sobre TODO lo anterior, user-secrets
                    // y variables de entorno incluidas: quien corre contra su propia base local empezaria
                    // a escribir en la de desarrollo sin enterarse. Re-agregar las dos fuentes personales
                    // les devuelve la precedencia.
                    config.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
