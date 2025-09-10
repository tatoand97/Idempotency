using Microsoft.EntityFrameworkCore;

namespace PruebaIdempotencia.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<IdempotencyRecordEntity> IdempotencyRecords => Set<IdempotencyRecordEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdempotencyRecordEntity>(b =>
        {
            b.ToTable("IdempotencyRecords");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.Operation, x.Key }).IsUnique();
            b.Property(x => x.Operation).IsRequired();
            b.Property(x => x.Key).IsRequired();
            b.Property(x => x.PayloadHash).IsRequired();
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt).IsRequired();
            b.Property(x => x.ExpiresAt).IsRequired();
        });

        modelBuilder.Entity<OrderEntity>(b =>
        {
            b.ToTable("Orders");
            b.HasKey(x => x.Id);
            b.HasIndex(x => x.OrderNumber).IsUnique();
            b.Property(x => x.OrderNumber).IsRequired();
            b.Property(x => x.Amount).IsRequired();
            b.Property(x => x.CreatedAt).IsRequired();
        });
    }
}
