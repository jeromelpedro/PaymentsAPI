using Azure.Messaging.ServiceBus;
using Payments.Api.Models;
using Payments.Api.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace Payments.Api.Consumers
{
	public class ServiceBusConsumer(ServiceBusClient _client, IPaymentService _paymentService, IServiceBus _serviceBus,
		IConfiguration _configuration, ILogger<ServiceBusConsumer> _logger) : BackgroundService
	{
		private ServiceBusProcessor _processor;

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			var queueNameOrderPlaced = _configuration["ServiceBus:QueueNameOrderPlaced"];

			_processor = _client.CreateProcessor(queueNameOrderPlaced, new ServiceBusProcessorOptions
			{
				MaxConcurrentCalls = 1,
				AutoCompleteMessages = false
			});

			_processor.ProcessMessageAsync += ProcessMessage;
			_processor.ProcessErrorAsync += ProcessError;

			await _processor.StartProcessingAsync(stoppingToken);

			_logger.LogInformation("ServiceBus Consumer iniciado");
		}

		private async Task ProcessMessage(ProcessMessageEventArgs args)
		{
			_logger.LogTrace("Mensagem recebida em ServiceBusConsumer");

			var json = Encoding.UTF8.GetString(args.Message.Body.ToArray());

			_logger.LogTrace("Payload recebido no consumidor com tamanho {Length}", json.Length);

			var message = JsonSerializer.Deserialize<OrderPlacedEvent>(json);

			await ProcessMessagePayment(message);

			await args.CompleteMessageAsync(args.Message);

		}

		private async Task ProcessMessagePayment(OrderPlacedEvent? message)
		{
			_logger.LogInformation("Consume iniciado para OrderId={orderId} UserId={userId} GameId={gameId} Price={price}",
							message.OrderId, message.UserId, message.GameId, message.Price);

			try
			{
				_logger.LogInformation("Processing payment for Order {OrderId}, User {UserId}, Game {GameId}, Amount {Price}",
					message.OrderId, message.UserId, message.GameId, message.Price);

				var isApproved = await _paymentService.ProcessPaymentAsync(
					message.OrderId,
					message.UserId,
					message.GameId,
					message.Price);

				var paymentEvent = new PaymentProcessedEvent
				{
					OrderId = message.OrderId,
					UserId = message.UserId,
					GameId = message.GameId,
					Price = message.Price,
					EmailUser = message.EmailUser,
					Status = isApproved ? PaymentStatus.Approved : PaymentStatus.Rejected
				};

				await _serviceBus.PublishAsync(_configuration["ServiceBus:QueueNamePaymentProcessed"], paymentEvent);

				_logger.LogInformation("Payment for Order {OrderId} processed with status: {Status}",
					message.OrderId, paymentEvent.Status);

				_logger.LogInformation("Consume concluído com sucesso para OrderId={orderId}", message.OrderId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro em Consume para OrderId={orderId}", message.OrderId);
				throw;
			}
		}

		private Task ProcessError(ProcessErrorEventArgs args)
		{
			_logger.LogError(args.Exception, "Erro no ServiceBus");
			return Task.CompletedTask;
		}
	}
}
