using Mapster;
using Wolverine;
using Wolverine.RabbitMQ;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Commands;
using OrderFlow.Application.Queries;
using OrderFlow.Infrastructure.Enums;
using OrderFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMq");
if  (string.IsNullOrWhiteSpace(rabbitMqConnectionString))
    throw new InvalidOperationException("RabbitMq connection string not set");

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(CreateOrderCommand).Assembly);
    opts.CodeGeneration.AlwaysUseServiceLocationFor<OrderFlowDbContext>();
    opts.UseRabbitMq(rabbitMqConnectionString).AutoProvision();
    opts.PublishMessage<OrderCreatedEvent>().ToRabbitQueue("order-created");
    opts.ListenToRabbitQueue("order-created");
});

TypeAdapterConfig.GlobalSettings.Scan(typeof(CreateOrderCommand).Assembly);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<OrderFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderFlowDb"))
);

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();



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

app.Run();

record CreateOrderRequest(string CustomerName, decimal TotalAmount);