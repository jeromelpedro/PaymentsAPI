namespace Payments.Api.Middlewares
{
  public class CorrelationIdMiddleware
  {
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeaderKey = "X-Correlation-Id";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      var correlationId = context.Request.Headers[CorrelationIdHeaderKey].FirstOrDefault() ?? Guid.NewGuid().ToString();

      context.Items[CorrelationIdHeaderKey] = correlationId;
      context.Response.Headers.Append(CorrelationIdHeaderKey, correlationId);

      await _next(context);
    }
  }

  public static class CorrelationIdMiddlewareExtensions
  {
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
      return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
  }
}
