namespace PruebaIdempotencia.Infrastructure;

public sealed class OrderEntity
{
    public Guid Id { get; set; }
    public required string OrderNumber { get; set; }
    public required decimal Amount { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}

