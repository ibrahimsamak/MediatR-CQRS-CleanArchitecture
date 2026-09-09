
namespace OrderFlow.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using OrderFlow.Domain.Orders;
using OrderFlow.Infrastructure.Persistence.Outbox;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
