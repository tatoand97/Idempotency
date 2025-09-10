using PruebaIdempotencia.Domain;

namespace PruebaIdempotencia.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind IdempotencyOptions from configuration (section optional). Defaults are set in the class.
        services.Configure<Domain.Idempotency.IdempotencyOptions>(configuration.GetSection("Idempotency"));
        services.AddScoped<ICreateOrderUseCase, Orders.CreateOrderUseCase>();
        return services;
    }
}
