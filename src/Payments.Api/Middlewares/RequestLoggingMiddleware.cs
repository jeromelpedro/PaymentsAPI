using System.Diagnostics;

namespace Payments.Api.Middlewares
{
  public class RequestLoggingMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
      _next = next;
      _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      const string headerKey = "X-Correlation-Id";
      var correlationId = context.Items[headerKey] as string ?? context.Request.Headers[headerKey].FirstOrDefault();
      var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

      _logger.LogInformation("-> {method} {path} | IP: {ip} | TraceId:{traceId} | CorrelationId:{correlationId}",
          context.Request.Method,
          context.Request.Path,
          context.Connection.RemoteIpAddress?.ToString(),
          traceId,
          correlationId);

      var stopwatch = Stopwatch.StartNew();

      await _next(context);

      stopwatch.Stop();

      _logger.LogInformation("<- {statusCode} | Tempo: {elapsed} ms | IP: {ip} | TraceId:{traceId} | CorrelationId:{correlationId}",
          context.Response.StatusCode,
          stopwatch.ElapsedMilliseconds,
          context.Connection.RemoteIpAddress?.ToString(),
          traceId,
          correlationId);
    }
  }

  public static class RequestLoggingMiddlewareExtensions
  {
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
      return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
  }
}
