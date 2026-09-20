using Mapster;
using OrderFlow.Application.Commands;
using OrderFlow.Application.Queries;
using OrderFlow.Infrastructure.Enums;
using Wolverine;

namespace OrderFlow.Api.Configurations;

public record CreateOrderRequest(string CustomerName, decimal TotalAmount);

public static class MapApiRoutes
{
    public static void MapApisRoutes(this WebApplication app)
    {
        app.MapPost("/orders", async (CreateOrderRequest request, IMessageBus messageBus) =>
        {
            var command = request.Adapt<CreateOrderCommand>();
            var orderId = await messageBus.InvokeAsync<Guid>(command);
            return Results.Created($"/orders/{orderId}", new { id = orderId });
        });

        app.MapGet("/orders/{id:guid}", async (Guid id, IMessageBus messageBus) =>
        {
            var query = new GetOrderQuery(id);
            var order = await messageBus.InvokeAsync<OrderResponse?>(query);
            return order == null ? Results.NotFound() : Results.Ok(order);
        });

        app.MapGet("/orders", async (OrderStatus status, IMessageBus messageBus) =>
        {
            var query = new GetOrdersByStatusQuery(status);
            var orders = await messageBus.InvokeAsync<List<OrderResponse>>(query);
            return Results.Ok(orders);
        });
        
        app.MapPost("/orders/{id:guid}/confirm", async (Guid id, IMessageBus messageBus) =>
        {
            var success = await messageBus.InvokeAsync<bool>(new ConfirmOrderCommand(id));
            return success ? Results.NoContent() : Results.NotFound();
        });

        app.MapPost("/orders/{id:guid}/cancel", async (Guid id, IMessageBus messageBus) =>
        {
            var success = await messageBus.InvokeAsync<bool>(new CancelOrderCommand(id));
            return success ? Results.NoContent() : Results.NotFound();
        });

        app.MapPost("/orders/{id:guid}/complete", async (Guid id, IMessageBus messageBus) =>
        {
            var success = await messageBus.InvokeAsync<bool>(new CompleteOrderCommand(id));
            return success ? Results.NoContent() : Results.NotFound();
        });
    }
}