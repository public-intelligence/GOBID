using IdentityHub.Application.Repositories;
using IdentityHub.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityHub.Application.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.TryAddScoped<IRepository<LegalEntity>, GenericRepository<LegalEntity>>();
        services.TryAddScoped<IRepository<Person>, GenericRepository<Person>>();

        return services;
    }
}
