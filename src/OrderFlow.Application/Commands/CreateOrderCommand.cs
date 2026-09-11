using OrderFlow.Infrastructure.Domain;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Application.Commands;

public record CreateOrderCommand(string CustomerName, decimal TotalAmount);

public static class CreateOrderCommandHandler
{
    public static async Task<Guid> Handle(CreateOrderCommand command, OrderFlowDbContext db)
    {
        var order = Order.Create(command.CustomerName, command.TotalAmount);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }
}