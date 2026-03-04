using System.Net;
using System.Text.Json;

namespace Payments.Api.Middlewares
{
  public class ErrorHandlingMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
      _next = next;
      _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      try
      {
        await _next(context);
      }
      catch (Exception ex)
      {
        const string headerKey = "X-Correlation-Id";
        var correlationId = context.Items[headerKey] as string ?? context.Request.Headers[headerKey].FirstOrDefault();
        var traceId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;

        _logger.LogError(ex, "Erro não tratado | Mensagem: {message} | TraceId: {traceId} | CorrelationId: {correlationId}",
            ex.Message, traceId, correlationId);

        await HandleExceptionAsync(context, ex);
      }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
      context.Response.ContentType = "application/json";
      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

      var response = new
      {
        statusCode = context.Response.StatusCode,
        message = "Erro interno do servidor.",
        detailedMessage = exception.Message
      };

      var jsonResponse = JsonSerializer.Serialize(response);
      return context.Response.WriteAsync(jsonResponse);
    }
  }

  public static class ErrorHandlingMiddlewareExtensions
  {
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
      return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
  }
}
