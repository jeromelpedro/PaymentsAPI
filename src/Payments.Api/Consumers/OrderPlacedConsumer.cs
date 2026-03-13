using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Payments.Api.Models;
using Payments.Api.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace Payments.Api.Consumers
{
	public class OrderPlacedConsumer : BackgroundService
	{
		private readonly IPaymentService _paymentService;
		private readonly IServiceBus _serviceBus;
		private readonly ILogger<OrderPlacedConsumer> _logger;
		private readonly ServiceBusSettings _settings;
		private readonly ServiceBusClient _serviceBusClient;
		private ServiceBusProcessor? _processor;

		public OrderPlacedConsumer(
			ServiceBusClient serviceBusClient,
			IPaymentService paymentService,
			ILogger<OrderPlacedConsumer> logger,
			IServiceBus serviceBus,
			IOptions<ServiceBusSettings> options)
		{
			_serviceBusClient = serviceBusClient;
			_paymentService = paymentService;
			_logger = logger;
			_serviceBus = serviceBus;
			_settings = options.Value;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_processor = _serviceBusClient.CreateProcessor(_settings.QueueNameOrderPlaced, new ServiceBusProcessorOptions
			{
				MaxConcurrentCalls = 1,
				AutoCompleteMessages = false
			});

			_processor.ProcessMessageAsync += ProcessMessageAsync;
			_processor.ProcessErrorAsync += ProcessErrorAsync;

			await _processor.StartProcessingAsync(stoppingToken);
			_logger.LogInformation("ServiceBus consumer iniciado para fila {QueueName}", _settings.QueueNameOrderPlaced);
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			if (_processor is not null)
			{
				await _processor.StopProcessingAsync(cancellationToken);
				await _processor.DisposeAsync();
			}

			await base.StopAsync(cancellationToken);
		}

		private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
		{
			var json = Encoding.UTF8.GetString(args.Message.Body.ToArray());
			var message = JsonSerializer.Deserialize<OrderPlacedEvent>(json);

			if (message is null)
			{
				_logger.LogWarning("Falha ao desserializar OrderPlacedEvent");
				await args.AbandonMessageAsync(args.Message);
				return;
			}

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

				await _serviceBus.PublishAsync(_settings.QueueNamePaymentProcessed, paymentEvent);

				_logger.LogInformation("Payment for Order {OrderId} processed with status: {Status}",
					message.OrderId, paymentEvent.Status);

				_logger.LogInformation("Consume concluído com sucesso para OrderId={orderId}", message.OrderId);
				await args.CompleteMessageAsync(args.Message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro em Consume para OrderId={orderId}", message.OrderId);
				await args.AbandonMessageAsync(args.Message);
			}
		}

		private Task ProcessErrorAsync(ProcessErrorEventArgs args)
		{
			_logger.LogError(args.Exception, "Erro no Service Bus para entidade {EntityPath}", args.EntityPath);
			return Task.CompletedTask;
		}
	}
}
