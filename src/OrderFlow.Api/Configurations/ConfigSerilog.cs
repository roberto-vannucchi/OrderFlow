using Serilog;
using Serilog.Sinks.Grafana.Loki;

namespace OrderFlow.Api.Configurations;

public static class ConfigSerilog
{
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var lokiUrl = builder.Configuration.GetConnectionString("Loki");
        if (string.IsNullOrWhiteSpace(lokiUrl))
            throw new InvalidOperationException("Loki connection string not set");

        builder.Host.UseSerilog((context, services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.GrafanaLoki(lokiUrl, labels:
                [
                    new LokiLabel { Key = "app", Value = "orderflow" }
                ]);
        });
    }
}