using Mapster;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Commands;
using OrderFlow.Application.Queries;
using OrderFlow.Infrastructure.Persistence;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWolverine(opts =>
{
    opts.Discovery.IncludeAssembly(typeof(CreateOrderCommand).Assembly);
    opts.CodeGeneration.AlwaysUseServiceLocationFor<OrderFlowDbContext>();
});

TypeAdapterConfig.GlobalSettings.Scan(typeof(CreateOrderCommand).Assembly);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<OrderFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderFlowDb")));

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

app.Run();

record CreateOrderRequest(string CustomerName, decimal TotalAmount);