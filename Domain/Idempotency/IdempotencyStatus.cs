namespace PruebaIdempotencia.Domain.Idempotency;

public enum IdempotencyStatus
{
    Processing,
    Succeeded,
    Failed
}

