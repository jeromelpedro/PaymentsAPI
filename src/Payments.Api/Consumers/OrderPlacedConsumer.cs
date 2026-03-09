using MassTransit;
using Microsoft.Extensions.Options;
using Payments.Api.Models;
using Payments.Api.Services.Interfaces;

namespace Payments.Api.Consumers
{
	public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
	{
		private readonly IPaymentService _paymentService;
		private readonly ILogger<OrderPlacedConsumer> _logger;
		private readonly ServiceBusSettings _settings;

		public OrderPlacedConsumer(
			IPaymentService paymentService,
			ILogger<OrderPlacedConsumer> logger,
			IOptions<ServiceBusSettings> options)
		{
			_paymentService = paymentService;
			_logger = logger;
			_settings = options.Value;
		}

		public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
		{
			var message = context.Message;

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

				await context.Publish(paymentEvent);

				_logger.LogInformation("Payment for Order {OrderId} processed with status: {Status} and published to topic {Topic}",
					message.OrderId, paymentEvent.Status, _settings.PaymentProcessedTopicName);

				_logger.LogInformation("Consume concluído com sucesso para OrderId={orderId}", message.OrderId);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro em Consume para OrderId={orderId}", message.OrderId);
				throw;
			}
		}
	}
}
