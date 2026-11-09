using Microsoft.EntityFrameworkCore;

namespace CoffeeBlazorApp.Data;

public sealed class CoffeeDbContext(DbContextOptions<CoffeeDbContext> options) : DbContext(options)
{
    public DbSet<CoffeeOrder> Orders => Set<CoffeeOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<CoffeeOrder>();

        order.HasKey(item => item.Id);
        order.HasIndex(item => item.OrderNumber).IsUnique();
        order.HasIndex(item => item.TrackingToken).IsUnique();
        order.Property(item => item.OrderNumber).HasMaxLength(12).IsRequired();
        order.Property(item => item.TrackingToken).HasMaxLength(64).IsRequired();
        order.Property(item => item.CoffeeType).HasMaxLength(80).IsRequired();
        order.Property(item => item.Size).HasMaxLength(30).IsRequired();
        order.Property(item => item.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
    }
}
