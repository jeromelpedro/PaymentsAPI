namespace Payments.Api.Models
{
  public class ServiceBusSettings
  {
    public string ConnectionString { get; set; } = string.Empty;
    public string QueueNameOrderPlaced { get; set; } = string.Empty;
    public string QueueNamePaymentProcessed { get; set; } = string.Empty;
  }
}
