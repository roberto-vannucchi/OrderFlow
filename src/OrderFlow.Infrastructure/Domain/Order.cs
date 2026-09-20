using OrderFlow.Infrastructure.Enums;

namespace OrderFlow.Infrastructure.Domain;

public sealed class Order
{
    public Guid Id { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Order()
    {
        
    }

    public static Order Create(string customerName, decimal totalAmount)
    {
        return new Order
        {
            Id = Guid.CreateVersion7(),
            CustomerName = customerName,
            Status = OrderStatus.Pending,
            TotalAmount = totalAmount,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm order in status {Status}.");

        Status = OrderStatus.Confirmed;
    }
    
    public void Complete()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot complete order in status {Status}.");

        Status = OrderStatus.Completed;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed order.");

        Status = OrderStatus.Cancelled;
    }
}

