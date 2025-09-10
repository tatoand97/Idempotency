namespace PruebaIdempotencia.Domain.Idempotency;

public interface IIdempotencyStore
{
    Task<AcquireResult> AcquireAsync(string op, string key, string payloadHash, DateTimeOffset now, TimeSpan ttl, TimeSpan processingTimeout, CancellationToken ct = default);
    Task MarkSucceededAsync(string op, string key, string payloadHash, string responseBodyJson, int responseStatusCode, DateTimeOffset now, CancellationToken ct = default);
    Task MarkFailedAsync(string op, string key, string payloadHash, DateTimeOffset now, CancellationToken ct = default);
    Task<int> CleanupExpiredAsync(DateTimeOffset now, CancellationToken ct = default);
}

