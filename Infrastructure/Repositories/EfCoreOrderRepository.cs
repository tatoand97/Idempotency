using Microsoft.EntityFrameworkCore;
using PruebaIdempotencia.Domain;

namespace PruebaIdempotencia.Infrastructure;

public sealed class EfCoreOrderRepository(AppDbContext db) : IOrderRepository
{
    public async Task<(bool created, Order order)> CreateOrGetAsync(string orderNumber, decimal amount, DateTimeOffset now, CancellationToken ct = default)
    {
        // Try to create
        var entity = new OrderEntity
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            Amount = amount,
            CreatedAt = now
        };

        db.Orders.Add(entity);
        try
        {
            await db.SaveChangesAsync(ct);
            return (true, Map(entity));
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            var existing = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
            if (existing is not null)
            {
                return (false, Map(existing));
            }
            throw; // unexpected
        }
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        var e = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
        return e is null ? null : Map(e);
    }

    private static Order Map(OrderEntity e) => new()
    {
        Id = e.Id,
        OrderNumber = e.OrderNumber,
        Amount = e.Amount,
        CreatedAt = e.CreatedAt
    };
}
