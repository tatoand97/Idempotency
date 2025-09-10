namespace PruebaIdempotencia.Infrastructure;

public sealed class IdempotencyRecordEntity
{
    public int Id { get; init; }
    public required string Operation { get; set; }
    public required string Key { get; set; }
    public required string PayloadHash { get; set; }
    public required int Status { get; set; } // 0=Processing,1=Succeeded,2=Failed
    public string? ResponseBodyJson { get; set; }
    public int? ResponseStatusCode { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
    public required DateTimeOffset UpdatedAt { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
}

