using System.Diagnostics;

namespace FCG.Games.Api.Middleware;

public class CorrelationIdMiddleware : IMiddleware
{
    private const string Header = "x-correlation-id";

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!context.Request.Headers.TryGetValue(Header, out var correlation))
        {
            correlation = Guid.NewGuid().ToString();
            context.Request.Headers[Header] = correlation;
        }

        context.Response.Headers[Header] = correlation;
        using var activity = new Activity("request-correlation");
        activity.SetParentId(correlation);
        await next(context);
    }
}
