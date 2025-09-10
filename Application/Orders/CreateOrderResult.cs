namespace PruebaIdempotencia.Application.Orders;

public sealed class CreateOrderResult
{
    public required int StatusCode { get; init; }
    public string? ResponseBodyJson { get; init; }
    public int? RetryAfterSeconds { get; init; }
}

