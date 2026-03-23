using System.Diagnostics;
using Serilog.Context;

namespace FCG.Games.Api.Middleware;

public class CorrelationIdMiddleware : IMiddleware
{
    private const string Header = "x-correlation-id";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Headers.TryGetValue(Header, out var correlation) || string.IsNullOrEmpty(correlation))
        {
            correlation = Guid.NewGuid().ToString();
            context.Request.Headers[Header] = correlation;
        }

        context.Response.Headers[Header] = correlation;

        // Store correlation ID in HttpContext for downstream use
        context.Items["CorrelationId"] = correlation.ToString();

        using (LogContext.PushProperty("CorrelationId", correlation.ToString()))
        {
            await next(context);
        }
    }
}
