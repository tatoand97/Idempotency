using Microsoft.EntityFrameworkCore;
using PruebaIdempotencia.Domain.Idempotency;
using PruebaIdempotencia.Domain;
using PruebaIdempotencia.Infrastructure.Stores;

namespace PruebaIdempotencia.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("Default") ?? "Data Source=idempotencia.db"));

        services.AddScoped<IIdempotencyStore, EfCoreIdempotencyStore>();
        services.AddScoped<IOrderRepository, EfCoreOrderRepository>();
        services.AddHostedService<ExpiredRecordsCleanupService>();
        return services;
    }
}
