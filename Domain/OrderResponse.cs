namespace PruebaIdempotencia.Domain;

public sealed class OrderResponse
{
    public required Guid Id { get; init; }
    public required string OrderNumber { get; init; }
    public required decimal Amount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

