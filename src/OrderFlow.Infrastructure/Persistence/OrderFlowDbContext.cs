using Microsoft.EntityFrameworkCore;
using OrderFlow.Infrastructure.Domain;

namespace OrderFlow.Infrastructure.Persistence;

public sealed class OrderFlowDbContext : DbContext
{
    public OrderFlowDbContext(DbContextOptions<OrderFlowDbContext> options) : base(options)
    {
        
    }

    public DbSet<Order> Orders => Set<Order>();
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrderFlowDbContext).Assembly);
    }
}