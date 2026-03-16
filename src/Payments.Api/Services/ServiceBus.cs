using Azure.Messaging.ServiceBus;
using Payments.Api.Services.Interfaces;
using System.Text.Json;

namespace Payments.Api.Services
{
	public class ServiceBus(ServiceBusClient _serviceBusClient, ILogger<ServiceBus> _logger) : IServiceBus
	{
		public async Task PublishAsync(string topic, object message)
		{
			var sender = _serviceBusClient.CreateSender(topic);

			try
			{
				var messageBody = JsonSerializer.Serialize(message);
				var sMessage = new ServiceBusMessage(messageBody)
				{
					ContentType = "application/json"
				};

				await sender.SendMessageAsync(sMessage);

				_logger.LogInformation($"Mensagem enviada para fila {topic}.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Erro ao enviar mensagem para Service Bus fila {topic}.");
				throw;
			}
			finally
			{
				await sender.DisposeAsync();
			}
		}
	}
}
