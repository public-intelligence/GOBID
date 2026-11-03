using System.Reflection;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using IdentityHub;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Microsoft.OpenApi;
using IdentityHub.Application.Interceptors;
using IdentityHub.Application.Extensions;
using IdentityHub.Application.Data;
using IdentityHub.ParameterTransformers;
using IdentityHub.Service.Mappers;
using IdentityHub.Service.Extensions;


var builder = WebApplication.CreateBuilder(args);

// ── Key Vault para desarrollo local ───────────────────────────────────────────
// Para que nadie tenga que guardar credenciales en su maquina: los secretos de trabajo local
// salen del vault de QA. DefaultAzureCredential usa la sesion de `az login`.
//
// Es opcional: sin `KeyVault:LocalUri` no pasa nada y todo sigue como antes. Se agrega despues de los appsettings, asi que llena los secretos que se vaciaron de
// ahi. Para pisar un valor puntual en tu maquina: dotnet user-secrets, o una variable de
// entorno; las dos ganan sobre el vault.
//
// En los ambientes desplegados esto NO corre: alli los secretos llegan como App Settings.
var uriDelVaultLocal = builder.Configuration["KeyVault:LocalUri"];
if (builder.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(uriDelVaultLocal))
{
    try
    {
        builder.Configuration.AddAzureKeyVault(
            new Uri(uriDelVaultLocal),
            new DefaultAzureCredential(),
            new AzureKeyVaultConfigurationOptions { Manager = new SecretosDelVaultLocal() });

        // El proveedor del vault se agrega al final, asi que gana sobre TODO lo anterior:
        // user-secrets y variables de entorno incluidas. Medido. Eso deja sin salida a quien
        // corre contra su propia base local, que empezaria a escribir en QA sin enterarse.
        // Re-agregar las dos fuentes personales les devuelve la precedencia. Quedan dos
        // proveedores del mismo tipo, lo que es inofensivo: gana el ultimo y leen lo mismo.
        builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
        builder.Configuration.AddEnvironmentVariables();
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException(
            $"No se pudo leer el Key Vault de desarrollo ({uriDelVaultLocal}). Revisa que " +
            "tengas sesion abierta con `az login` y el rol 'Key Vault Secrets User' sobre ese " +
            "vault. Si no lo necesitas, quita la clave 'KeyVault:LocalUri' de tu configuracion " +
            "local.", ex);
    }
}

// FAIL-CLOSED. El guard anterior era `?? throw`, y `??` solo atrapa null: una cadena vacia lo
// atraviesa y revienta mas abajo con un error que no nombra la clave. Importa ahora que los
// secretos se van a vaciar de los appsettings: vacio pasa a ser el estado normal de un clone sin
// configurar, y ahi la diferencia entre "no arranca y te dice que falta DefaultConnection" y
// "revienta en el primer query" son horas de diagnostico.
var cadenaDeConexion = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(cadenaDeConexion))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection esta vacia o ausente y la API no arranca sin ella. " +
        "En local: dotnet user-secrets set \"KeyVault:LocalUri\" " +
        "\"https://kv-shared-vuc-dev.vault.azure.net/\" --project IdentityHub (previo `az login`), " +
        "o pedi el valor a quien administra la configuracion. " +
        "En los ambientes desplegados: en la configuracion del App Service.");
}

builder.Services.AddDbContext<IdentityHubDbContext>(
    (sp, options) =>
    {
        options
            .UseSqlServer(
                cadenaDeConexion,
                opt => opt.MigrationsAssembly("IdentityHub.Application")
            )
            .AddInterceptors(sp.GetRequiredService<SoftDeleteInterceptor>());
    }
);

// Add services to the container.
builder.Services.AddControllers(options =>
    options.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()))
)
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            [.. builder
                .Configuration.GetRequiredSection("AllowedClientOrigins")
                .Get<List<string>>()!]
        );
        policy.AllowAnyHeader();
        policy.AllowAnyMethod();
    });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnetcore/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Version = "v1",
            Title = "RRHH API",
            Description = "API REST del Sistema de Recursos Humanos.",
        };

        // Configurar esquema de seguridad Bearer
        var bearerScheme = new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "JSON Web Token (JWT)",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        };

        document.Components ??= new();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = bearerScheme;

        var securityRequirement = new OpenApiSecurityRequirement();
        var securitySchemeReference = new OpenApiSecuritySchemeReference("Bearer", document);
        securityRequirement[securitySchemeReference] = new List<string>();

        foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations.Values))
        {
            operation.Security = new List<OpenApiSecurityRequirement> { securityRequirement };
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddUnitOfWork<IdentityHubDbContext>();
builder.Services.AddRepositories();
builder.Services.AddServices();

// Health check para la compuerta de despliegue. AddDbContextCheck abre la conexion de verdad,
// asi que /health distingue dos fallas que desde afuera se parecen: si devuelve 200 la
// aplicacion arranco Y la base responde; si devuelve 503 el proceso esta vivo pero la base no
// contesta —cadena de conexion mala, o una referencia al vault sin resolver—. Sin esa
// distincion, un despliegue "exitoso" puede dejar el servicio inutil sin que nadie se entere:
// la tarea AzureWebApp reporta exito al COPIAR el paquete, no al arrancarlo.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<IdentityHubDbContext>("base-de-datos");

builder.Services.AddSingleton<SoftDeleteInterceptor>();

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MapperProfile>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Anonimo a proposito: lo sondea el pipeline de despliegue, que no tiene token. No expone nada
// sensible: el cuerpo es "Healthy" o "Unhealthy", sin detalle de la base.
app.MapHealthChecks("/health").AllowAnonymous();

app.Run();
