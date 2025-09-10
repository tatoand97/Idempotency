using PruebaIdempotencia.Application.Orders;

namespace PruebaIdempotencia.Domain;

public interface ICreateOrderUseCase
{
    Task<CreateOrderResult> ExecuteAsync(OrderRequest request, string idempotencyKey, CancellationToken ct = default);
}

