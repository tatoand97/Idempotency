namespace PruebaIdempotencia.Domain.Idempotency;

public sealed class IdempotencyRecord
{
    public required string Id { get; init; }
    public required string Key { get; init; }
    public required string Operation { get; init; }
    public required string PayloadHash { get; init; }
    public IdempotencyStatus Status { get; set; }
    public string? ResponseBodyJson { get; set; }
    public int? ResponseStatusCode { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

