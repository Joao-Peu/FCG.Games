using System.Diagnostics;
using FCG.Games.Api.Diagnostics;

namespace FCG.Games.Api.Middleware;

public class MetricsMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var tags = new TagList
        {
            { "http.method", context.Request.Method },
            { "http.route", context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value }
        };

        GameMetrics.HttpRequestsTotal.Add(1, tags);

        var sw = Stopwatch.StartNew();
        try
        {
            await next(context);
        }
        finally
        {
            sw.Stop();
            tags.Add("http.status_code", context.Response.StatusCode);
            GameMetrics.HttpRequestDuration.Record(sw.Elapsed.TotalMilliseconds, tags);
        }
    }
}
