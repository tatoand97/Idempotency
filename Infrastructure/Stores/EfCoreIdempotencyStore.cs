using Microsoft.EntityFrameworkCore;
using PruebaIdempotencia.Domain.Idempotency;

namespace PruebaIdempotencia.Infrastructure.Stores;

public sealed class EfCoreIdempotencyStore(AppDbContext db) : IIdempotencyStore
{
    public async Task<AcquireResult> AcquireAsync(string op, string key, string payloadHash, DateTimeOffset now, TimeSpan ttl, TimeSpan processingTimeout, CancellationToken ct = default)
    {
        // Try insert (set-on-insert)
        var entity = new IdempotencyRecordEntity
        {
            Operation = op,
            Key = key,
            PayloadHash = payloadHash,
            Status = (int)IdempotencyStatus.Processing,
            CreatedAt = now,
            UpdatedAt = now,
            ExpiresAt = now.Add(ttl)
        };

        try
        {
            db.IdempotencyRecords.Add(entity);
            await db.SaveChangesAsync(ct);
            return new AcquireResult { Outcome = AcquireOutcome.FirstProcessing };
        }
        catch (DbUpdateException)
        {
            // Likely unique index violation – record exists
            db.ChangeTracker.Clear();
        }

        var existing = await db.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Operation == op && r.Key == key, ct);

        if (existing is null)
        {
            // race: try again
            return new AcquireResult { Outcome = AcquireOutcome.ExistsProcessing, RetryAfter = TimeSpan.FromSeconds(1) };
        }

        if (!string.Equals(existing.PayloadHash, payloadHash, StringComparison.Ordinal))
        {
            return new AcquireResult { Outcome = AcquireOutcome.ConflictDifferentPayload, Existing = Map(existing) };
        }

        switch (existing.Status)
        {
            case (int)IdempotencyStatus.Succeeded:
                return new AcquireResult { Outcome = AcquireOutcome.ExistsSucceeded, Existing = Map(existing) };
            case (int)IdempotencyStatus.Processing:
            {
                var age = now - existing.UpdatedAt;
                if (age > processingTimeout)
                {
                    // Attempt takeover via CAS (UpdatedAt and Status must match)
                    var rows = await db.IdempotencyRecords
                        .Where(r => r.Operation == op && r.Key == key && r.PayloadHash == payloadHash && r.Status == (int)IdempotencyStatus.Processing && r.UpdatedAt == existing.UpdatedAt)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(r => r.UpdatedAt, now)
                            .SetProperty(r => r.ExpiresAt, now.Add(ttl)), ct);

                    return rows > 0 ? new AcquireResult { Outcome = AcquireOutcome.FirstProcessing } :
                        // Someone else took it
                        new AcquireResult { Outcome = AcquireOutcome.ExistsProcessing, Existing = Map(existing), RetryAfter = TimeSpan.FromSeconds(5) };
                }

                var retryAfter = processingTimeout - age;
                if (retryAfter < TimeSpan.Zero) retryAfter = TimeSpan.FromSeconds(1);
                return new AcquireResult { Outcome = AcquireOutcome.ExistsProcessing, Existing = Map(existing), RetryAfter = retryAfter };
            }
            case (int)IdempotencyStatus.Failed:
            {
                // Flip to Processing (best-effort)
                var rows = await db.IdempotencyRecords
                    .Where(r => r.Operation == op && r.Key == key && r.PayloadHash == payloadHash && r.Status == (int)IdempotencyStatus.Failed)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(r => r.Status, (int)IdempotencyStatus.Processing)
                        .SetProperty(r => r.UpdatedAt, now)
                        .SetProperty(r => r.ExpiresAt, now.Add(ttl)), ct);

                return rows > 0 ? new AcquireResult { Outcome = AcquireOutcome.FirstProcessing } : new AcquireResult { Outcome = AcquireOutcome.ExistsProcessing, Existing = Map(existing), RetryAfter = TimeSpan.FromSeconds(3) };
            }
            default:
                return new AcquireResult { Outcome = AcquireOutcome.ExistsProcessing, Existing = Map(existing), RetryAfter = TimeSpan.FromSeconds(3) };
        }
    }

    public async Task MarkSucceededAsync(string op, string key, string payloadHash, string responseBodyJson, int responseStatusCode, DateTimeOffset now, CancellationToken ct = default)
    {
        await db.IdempotencyRecords
            .Where(r => r.Operation == op && r.Key == key && r.PayloadHash == payloadHash)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, (int)IdempotencyStatus.Succeeded)
                .SetProperty(r => r.ResponseBodyJson, responseBodyJson)
                .SetProperty(r => r.ResponseStatusCode, responseStatusCode)
                .SetProperty(r => r.UpdatedAt, now), ct);
    }

    public async Task MarkFailedAsync(string op, string key, string payloadHash, DateTimeOffset now, CancellationToken ct = default)
    {
        await db.IdempotencyRecords
            .Where(r => r.Operation == op && r.Key == key && r.PayloadHash == payloadHash)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.Status, (int)IdempotencyStatus.Failed)
                .SetProperty(r => r.UpdatedAt, now), ct);
    }

    public async Task<int> CleanupExpiredAsync(DateTimeOffset now, CancellationToken ct = default)
    {
        return await db.IdempotencyRecords
            .Where(r => r.ExpiresAt <= now)
            .ExecuteDeleteAsync(ct);
    }

    private static IdempotencyRecord Map(IdempotencyRecordEntity e) => new()
    {
        Id = $"{e.Operation}:{e.Key}",
        Key = e.Key,
        Operation = e.Operation,
        PayloadHash = e.PayloadHash,
        Status = (IdempotencyStatus)e.Status,
        ResponseBodyJson = e.ResponseBodyJson,
        ResponseStatusCode = e.ResponseStatusCode,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt,
        ExpiresAt = e.ExpiresAt
    };
}
