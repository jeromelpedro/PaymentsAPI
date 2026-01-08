using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Payments.Api.Models;
using Payments.Api.Services.Interfaces;

namespace Payments.Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class PaymentsController : ControllerBase
	{
		private readonly IPaymentService _paymentService;
		private readonly IRabbitMqPublisher _rabbitMqPublisher;
		private readonly RabbitMqSettings _settings;

		public PaymentsController(
			IPaymentService paymentService,
			IRabbitMqPublisher rabbitMqPublisher,
			IOptions<RabbitMqSettings> options)
		{
			_paymentService = paymentService;
			_rabbitMqPublisher = rabbitMqPublisher;
			_settings = options.Value;
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

			await _rabbitMqPublisher.PublishAsync(paymentEvent, _settings.QueueNamePaymentProcessed);

			if (isApproved)
			{
				return Ok(new { message = "Pagamento aprovado com sucesso.", orderId = request.OrderId });
			}

			return BadRequest(new { message = "Pagamento recusado.", orderId = request.OrderId });
		}

		[HttpGet("health")]
		[AllowAnonymous]
		public IActionResult Health()
		{
			return Ok(new { status = "healthy", service = "PaymentsAPI" });
		}
	}
}
