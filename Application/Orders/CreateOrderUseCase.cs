using Microsoft.Extensions.Options;
using PruebaIdempotencia.Domain;
using PruebaIdempotencia.Domain.Idempotency;
using System.Text.Json;

namespace PruebaIdempotencia.Application.Orders;

public sealed class CreateOrderUseCase(IOrderRepository repo, IIdempotencyStore store, IOptions<IdempotencyOptions> options)
    : ICreateOrderUseCase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<CreateOrderResult> ExecuteAsync(OrderRequest request, string idempotencyKey, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        const string op = "CreateOrder";

        // Hash payload deterministically
        var canonicalJson = JsonSerializer.Serialize(request, JsonOpts);
        var payloadHash = Hashing.Sha256Base64(canonicalJson);

        var acq = await store.AcquireAsync(op, idempotencyKey, payloadHash, now, options.Value.Ttl, options.Value.ProcessingTimeout, ct);
        switch (acq.Outcome)
        {
            case AcquireOutcome.ConflictDifferentPayload:
                return new CreateOrderResult { StatusCode = 409 };
            case AcquireOutcome.ExistsSucceeded:
            {
                var r = acq.Existing!;
                return new CreateOrderResult
                {
                    StatusCode = r.ResponseStatusCode ?? 200,
                    ResponseBodyJson = r.ResponseBodyJson ?? "{}"
                };
            }
            case AcquireOutcome.ExistsProcessing:
            {
                var retryAfter = acq.RetryAfter.HasValue ? (int)Math.Ceiling(acq.RetryAfter.Value.TotalSeconds) : (int?)null;
                return new CreateOrderResult { StatusCode = 409, RetryAfterSeconds = retryAfter };
            }
            case AcquireOutcome.ExistsFailed:
            case AcquireOutcome.FirstProcessing:
                break; // proceed
            default:
                return new CreateOrderResult { StatusCode = 500 };
        }

        // Domain effect guarded by unique constraint
        var (created, order) = await repo.CreateOrGetAsync(request.OrderNumber, request.Amount, now, ct);
        var response = new OrderResponse
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Amount = order.Amount,
            CreatedAt = order.CreatedAt
        };
        var responseJson = JsonSerializer.Serialize(response, JsonOpts);

        var statusCode = created ? 201 : 200;
        await store.MarkSucceededAsync(op, idempotencyKey, payloadHash, responseJson, statusCode, DateTimeOffset.UtcNow, ct);
        return new CreateOrderResult { StatusCode = statusCode, ResponseBodyJson = responseJson };
    }
}

