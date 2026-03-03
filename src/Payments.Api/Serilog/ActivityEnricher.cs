using Serilog.Core;
using Serilog.Events;
using System.Diagnostics;

namespace Payments.Api.Serilog
{
  public class ActivityEnricher : ILogEventEnricher
  {
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
      var activity = Activity.Current;
      if (activity == null) return;

      if (activity.TraceId != default)
      {
        var traceProp = propertyFactory.CreateProperty("TraceId", activity.TraceId.ToString());
        logEvent.AddPropertyIfAbsent(traceProp);
      }

      if (activity.SpanId != default)
      {
        var spanProp = propertyFactory.CreateProperty("SpanId", activity.SpanId.ToString());
        logEvent.AddPropertyIfAbsent(spanProp);
      }
    }
  }
}
