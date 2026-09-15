using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Mapster;
using OrderFlow.Infrastructure.Enums;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Application.Queries;

public record GetOrdersByStatusQuery(OrderStatus Status);

public static class GetOrdersByStatusQueryHandler
{
    public static async Task<List<OrderResponse>> Handle(GetOrdersByStatusQuery query, OrderFlowDbContext db, IDistributedCache cache)
    {
        var cacheKey = $"orders:status:{query.Status}";

        var cached = await cache.GetStringAsync(cacheKey);
        if (cached is not null)
            return JsonSerializer.Deserialize<List<OrderResponse>>(cached)!;

        var orders = await db.Orders
            .AsNoTracking()
            .Where(o => o.Status == query.Status)
            .ToListAsync();

        var response = orders.Adapt<List<OrderResponse>>();

        return response;
    }
}