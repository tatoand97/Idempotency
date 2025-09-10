namespace PruebaIdempotencia.Domain;

public sealed class OrderRequest
{
    public required string OrderNumber { get; init; }
    public required decimal Amount { get; init; }
}

