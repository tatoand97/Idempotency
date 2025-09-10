namespace PruebaIdempotencia.Domain.Idempotency;

public sealed class AcquireResult
{
    public required AcquireOutcome Outcome { get; init; }
    public IdempotencyRecord? Existing { get; init; }
    public TimeSpan? RetryAfter { get; init; }
}

