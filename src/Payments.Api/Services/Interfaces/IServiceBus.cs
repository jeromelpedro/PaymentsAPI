namespace Payments.Api.Services.Interfaces
{
  public interface IServiceBus
  {
    Task PublishAsync(string queueName, object message);
  }
}
