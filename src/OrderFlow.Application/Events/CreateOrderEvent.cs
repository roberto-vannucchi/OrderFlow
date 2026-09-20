using System.Text.Json;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OrderFlow.Application.Queries;
using OrderFlow.Infrastructure.Enums;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Application.Events;

public record OrderCreatedEvent;

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