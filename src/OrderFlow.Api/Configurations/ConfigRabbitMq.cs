using Wolverine;
using Wolverine.RabbitMQ;
using OrderFlow.Application.Commands;
using OrderFlow.Application.Events;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.Api.Configurations;

public static class ConfigRabbitMq
{
    public static void ConfigureRabbitMq(this WebApplicationBuilder builder)
    {
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
            
            opts.PublishMessage<OrderStatusChangedEvent>().ToRabbitQueue("order-updated");
            opts.ListenToRabbitQueue("order-updated");
        });
    }
}