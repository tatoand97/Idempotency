namespace PruebaIdempotencia.Domain.Idempotency;

public sealed class IdempotencyOptions
{
    public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(48);
    public TimeSpan ProcessingTimeout { get; set; } = TimeSpan.FromMinutes(10);
}

