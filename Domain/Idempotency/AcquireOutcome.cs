namespace PruebaIdempotencia.Domain.Idempotency;

public enum AcquireOutcome
{
    FirstProcessing,
    ExistsSucceeded,
    ExistsProcessing,
    ExistsFailed,
    ConflictDifferentPayload
}

