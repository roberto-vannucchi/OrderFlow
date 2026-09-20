using System.Text.Json;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using OrderFlow.Application.Queries;
using OrderFlow.Infrastructure.Enums;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Application.Events;

public record OrderStatusChangedEvent(OrderStatus OldOrderStatus, OrderStatus NewOrderStatus);

public static class OrderStatusChangedEventHandler
{
     public static async Task Handle(OrderStatusChangedEvent @event, OrderFlowDbContext db, IDistributedCache cache)
     {
          var oldStatusList = await db.Orders.AsNoTracking().Where(o => o.Status == @event.OldOrderStatus).ToListAsync();
          var newStatusList = await db.Orders.AsNoTracking().Where(o => o.Status == @event.NewOrderStatus).ToListAsync(); 
          
          var cacheKeyOldStatus = $"orders:status:{@event.OldOrderStatus}";
          var cacheKeyNewStatus = $"orders:status:{@event.NewOrderStatus}";

          await cache.SetStringAsync(cacheKeyOldStatus,
               JsonSerializer.Serialize(oldStatusList.Adapt<List<OrderResponse>>()));
          
          await cache.SetStringAsync(cacheKeyNewStatus,
               JsonSerializer.Serialize(newStatusList.Adapt<List<OrderResponse>>()));
     }
}