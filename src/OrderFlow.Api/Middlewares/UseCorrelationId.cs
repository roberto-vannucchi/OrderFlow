using Serilog.Context;

namespace OrderFlow.Api.Middlewares;

public static class UseCorrelationId
{
    public static void UseCorrelationIdMiddleware(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers.TryGetValue("X-Correlation-Id", out var existing)
                ? existing.ToString()
                : Guid.CreateVersion7().ToString();

            context.Response.Headers["X-Correlation-Id"] = correlationId;

            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                await next();
            }
        });
    }
}