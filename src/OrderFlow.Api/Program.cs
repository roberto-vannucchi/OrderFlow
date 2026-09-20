using Microsoft.EntityFrameworkCore;
using Mapster;
using Scalar.AspNetCore;
using OrderFlow.Api.Configurations;
using OrderFlow.Application.Commands;
using OrderFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureRabbitMq();

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
    app.MapScalarApiReference();   
}

app.UseHttpsRedirection();


app.MapApisRoutes();


app.Run();
