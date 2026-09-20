using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Events;
using OrderFlow.Infrastructure.Persistence;
using Wolverine;

namespace OrderFlow.Application.Commands;

public record CompleteOrderCommand(Guid Id);

public static class CompleteOrderCommandHandler
{
    public static async Task<bool> Handle(CompleteOrderCommand request, OrderFlowDbContext db, IMessageBus bus, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken: cancellationToken);
        
        if (order == null)
            return false;

        var oldStatus = order.Status;
        
        order.Complete();
        await db.SaveChangesAsync(cancellationToken);
        
        await bus.PublishAsync(new OrderStatusChangedEvent(oldStatus, order.Status));
        
        return true;

    }
}