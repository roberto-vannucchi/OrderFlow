using System.Text.Json;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OrderFlow.Application.Queries;
using Wolverine;
using OrderFlow.Infrastructure.Domain;
using OrderFlow.Infrastructure.Enums;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Application.Commands;

public record CreateOrderCommand(string CustomerName, decimal TotalAmount);
public record OrderCreatedEvent();

public static class CreateOrderCommandHandler
{
    public static async Task<Guid> Handle(CreateOrderCommand command, OrderFlowDbContext db, IMessageBus bus)
    {
        var order = Order.Create(command.CustomerName, command.TotalAmount);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        
        await bus.PublishAsync(new OrderCreatedEvent());
        
        return order.Id;
    }
}

public static class OrderCreatedEventHandler
{
    public static async Task Handle(OrderCreatedEvent @event, OrderFlowDbContext db, IDistributedCache cache)
    {
        var pendingOrders = await db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Pending)
            .ToListAsync();

        var response = pendingOrders.Adapt<List<OrderResponse>>();

        await cache.SetStringAsync($"orders:status:{OrderStatus.Pending}", JsonSerializer.Serialize(response));
    }
}