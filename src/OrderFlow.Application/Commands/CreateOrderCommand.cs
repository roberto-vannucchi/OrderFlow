using OrderFlow.Application.Events;
using Wolverine;
using OrderFlow.Infrastructure.Domain;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Application.Commands;

public record CreateOrderCommand(string CustomerName, decimal TotalAmount);

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