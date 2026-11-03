using GOBID.Application.Interfaces;
using GOBID.Application.Interfaces.Repositories;
using GOBID.Application.Interfaces.Services;
using GOBID.Infrastructure.Data;
using GOBID.Infrastructure.Repositories;
using GOBID.Infrastructure.Security;
using GOBID.Infrastructure.Services;
using GOBID.Infrastructure.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data;

namespace GOBID.Infrastructure
{
    public static class ServiceExtensions
    {
        public static void AddPersistenceInfraestructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<PrincipalContext>(options => options.UseSqlServer(
                configuration.GetConnectionString("default"),
                b => b.MigrationsAssembly(typeof(PrincipalContext).Assembly.FullName)));

            services.AddScoped<DbContext, PrincipalContext>();

            //SI necesito usar dapper o otro dataAccess solo es quitar este comentario//
            services.AddScoped<IDbConnection>(db =>
              new SqlConnection(configuration.GetConnectionString("default")));

            services.AddScoped<ISecondaryDbConnection>(db =>
               new SecondaryDbConnection(configuration.GetConnectionString("secondaryDb") ?? string.Empty));


            #region Repositories
            services.AddScoped(typeof(IRepositoryAsync<>), typeof(EFRepository<>));


            #endregion

            #region Services 
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IApplicationAccessService, ApplicationAccessService>();
            services.AddScoped<IOrganizationService, OrganizationService>();
            services.AddScoped<IOrganizationApplicationService, OrganizationApplicationService>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IAccessTypeService, AccessTypeService>();
            services.AddScoped<IClaimService, ClaimService>();
            services.AddScoped<IUserApplicationService, UserApplicationService>();
            services.AddScoped<IOrganizationContactService, OrganizationContactService>();
            //services.AddScoped<IOrganizationUiCustomizationService, OrganizationUiCustomizationService>();
            services.AddScoped<IUserRoleService, UserRoleService>();
            //services.AddScoped<IChangeLogService, ChangeLogService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IRoleClaimService, RoleClaimService>();
            services.AddScoped<IDashboardService, DashBoardService>();
            services.AddScoped<ILegalEntityService, LegalEntityService>();

            // M2M: gestión de credenciales de aplicaciones
            services.AddScoped<IApplicationCredentialService, ApplicationCredentialService>();

            // El resolver de clave de firma M2M (AppJwtSigningKeyResolver) NO se registra acá:
            // lo registra Startup con la misma instancia que usa la validación de tokens. Un
            // AddSingleton por tipo creaba una segunda instancia con su propia caché de claves.

            // M2M: gestión de roles de aplicaciones (service accounts)
            services.AddScoped<IApplicationRoleService, ApplicationRoleService>();

            // M2M: gestión de claims de roles de aplicaciones
            services.AddScoped<IApplicationRoleClaimService, ApplicationRoleClaimService>();

            // ABAC: políticas de acceso
            services.AddScoped<IPolicyTypeService, PolicyTypeService>();
            services.AddScoped<IPolicyService, PolicyService>();
            services.AddScoped<IUserApplicationPolicyService, UserApplicationPolicyService>();
            services.AddScoped<IPolicyEvaluatorService, PolicyEvaluatorService>();

            // Registrar HttpClient para PersonApiService
            services.AddHttpClient<IPersonApiService, PersonApiService>();

            // Registrar HttpClient para PdsApiService
            services.AddMemoryCache();
            services.AddHttpClient<IPdsApiService, PdsApiService>();

            services.AddScoped<IPersonnelService, PersonnelService>();
            #endregion
        }

        public static IServiceCollection AddEmailService(
            this IServiceCollection services,
            GraphMailSettings graphMailSettings)
        {
            services.AddSingleton(graphMailSettings);
            services.AddScoped<IEmailService, EmailService>();

            return services;
        }
    }
}
