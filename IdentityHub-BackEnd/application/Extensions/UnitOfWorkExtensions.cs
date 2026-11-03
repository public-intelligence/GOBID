using IdentityHub.Application.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityHub.Application.Extensions;

public static class UnitOfWorkExtensions
{
    public static IServiceCollection AddUnitOfWork<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext
    {
        services.AddScoped<IUnitOfWork<TDbContext>, UnitOfWork<TDbContext>>();
        return services;
    }
}