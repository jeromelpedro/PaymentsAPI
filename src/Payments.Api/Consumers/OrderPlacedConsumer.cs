using MassTransit;
using Payments.Api.Models;
using Payments.Api.Services.Interfaces;

namespace Payments.Api.Consumers
{
	public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
	{
		private readonly IPaymentService _paymentService;
		private readonly IPublishEndpoint _publishEndpoint;
		private readonly ILogger<OrderPlacedConsumer> _logger;

		public OrderPlacedConsumer(
			IPaymentService paymentService, 
			IPublishEndpoint publishEndpoint,
			ILogger<OrderPlacedConsumer> logger)
		{
			_paymentService = paymentService;
			_publishEndpoint = publishEndpoint;
			_logger = logger;
		}

		public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
		{
			var message = context.Message;

			_logger.LogInformation("Processing payment for Order {OrderId}, User {UserId}, Amount {Price}", 
				message.OrderId, message.UserId, message.Price);

			var isApproved = await _paymentService.ProcessPaymentAsync(message.OrderId, message.UserId, message.Price);

			var paymentEvent = new PaymentProcessedEvent
			{
				OrderId = message.OrderId,
				UserId = message.UserId,
				UserEmail = message.UserEmail,
				Amount = message.Price,
				Status = isApproved ? "Approved" : "Declined"
			};

			await _publishEndpoint.Publish(paymentEvent);

			_logger.LogInformation("Payment for Order {OrderId} processed with status: {Status}", 
				message.OrderId, paymentEvent.Status);
		}
	}
}
