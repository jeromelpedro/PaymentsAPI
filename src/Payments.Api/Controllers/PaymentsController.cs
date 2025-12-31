using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Payments.Api.Models;
using Payments.Api.Services.Interfaces;
using static MassTransit.Monitoring.Performance.BuiltInCounters;

namespace Payments.Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PaymentsController : ControllerBase
	{
		private readonly IPaymentService _paymentService;
		private readonly IRabbitMqPublisher _rabbitMqPublisher;


		public PaymentsController(IPaymentService paymentService, IRabbitMqPublisher rabbitMqPublisher)
		{
			_paymentService = paymentService;
			_rabbitMqPublisher = rabbitMqPublisher;
		}

		[HttpPost("process")]
		public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
		{
			var isApproved = await _paymentService.ProcessPaymentAsync(
				request.OrderId,
				request.UserId,
				request.GameId,
				request.Price);

			var paymentEvent = new PaymentProcessedEvent
			{
				OrderId = request.OrderId,
				UserId = request.UserId,
				GameId = request.GameId,
				Price = request.Price,
				EmailUser = request.EmailUser,
				Status = isApproved ? PaymentStatus.Approved : PaymentStatus.Rejected
			};

			await _rabbitMqPublisher.PublishAsync(paymentEvent, "PaymentProcessedEvent");

			if (isApproved)
			{
				return Ok(new { message = "Pagamento aprovado com sucesso.", orderId = request.OrderId });
			}

			return BadRequest(new { message = "Pagamento recusado.", orderId = request.OrderId });
		}

		[HttpGet("health")]
		public IActionResult Health()
		{
			return Ok(new { status = "healthy", service = "PaymentsAPI" });
		}
	}
}
