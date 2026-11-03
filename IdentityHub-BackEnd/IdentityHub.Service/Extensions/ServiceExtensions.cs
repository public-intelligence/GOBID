using IdentityHub.Service.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityHub.Service.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.TryAddScoped<IOrganizationServices, LegalEntityServices>();
        services.TryAddScoped<IPersonServices, PersonServices>();
        services.TryAddScoped<IPaginationService, PaginationService>();
        return services;
    }
}
