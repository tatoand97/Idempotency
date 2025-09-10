namespace PruebaIdempotencia.Domain;

public interface IOrderRepository
{
    Task<(bool created, Order order)> CreateOrGetAsync(string orderNumber, decimal amount, DateTimeOffset now, CancellationToken ct = default);
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);
}

