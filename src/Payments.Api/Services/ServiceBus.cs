using Azure.Messaging.ServiceBus;
using Payments.Api.Services.Interfaces;
using System.Text.Json;

namespace Payments.Api.Services
{
  public class ServiceBus(ServiceBusClient serviceBusClient, ILogger<ServiceBus> logger) : IServiceBus
  {
    public async Task PublishAsync(string queueName, object message)
    {
      var sender = serviceBusClient.CreateSender(queueName);

      try
      {
        var messageBody = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(messageBody)
        {
          ContentType = "application/json"
        };

        await sender.SendMessageAsync(serviceBusMessage);
        logger.LogInformation("Mensagem enviada para fila {QueueName}", queueName);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Erro ao enviar mensagem para Service Bus fila {QueueName}", queueName);
        throw;
      }
      finally
      {
        await sender.DisposeAsync();
      }
    }
  }
}
