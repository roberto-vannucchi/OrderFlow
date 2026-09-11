using Microsoft.EntityFrameworkCore;
using Mapster;
using OrderFlow.Infrastructure.Domain;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Application.Queries;

public record GetOrderQuery(Guid Id);

public record OrderResponse(
    Guid Id, 
    string CustomerName, 
    string OrderReference, 
    string Status, 
    decimal TotalAmount, 
    DateTimeOffset CreatedAt);

public static class GetOrderQueryHandler
{
    public static async Task<OrderResponse?> Handle(GetOrderQuery query, OrderFlowDbContext db)
    {
        var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == query.Id);
        return order?.Adapt<OrderResponse>();
    }
}

public class OrderMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Order, OrderResponse>()
            .Map(dest => dest.OrderReference,
                src => $"ORD-{src.CreatedAt:yyyyMMdd}-{src.Id.ToString().Substring(0, 8).ToUpper()}");
    }
}