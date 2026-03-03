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
		private readonly ILogger<PaymentsController> _logger;

		public PaymentsController(
			IPaymentService paymentService,
			IRabbitMqPublisher rabbitMqPublisher,
			IOptions<RabbitMqSettings> options,
			ILogger<PaymentsController> logger)
		{
			_paymentService = paymentService;
			_rabbitMqPublisher = rabbitMqPublisher;
			_settings = options.Value;
			_logger = logger;
		}

		[HttpPost("process")]
		public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
		{
			_logger.LogInformation("ProcessPayment iniciado para OrderId={orderId} UserId={userId} GameId={gameId} Price={price}",
				request.OrderId, request.UserId, request.GameId, request.Price);

			try
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

				_logger.LogInformation("Evento PaymentProcessed publicado para OrderId={orderId} Status={status}",
					request.OrderId, paymentEvent.Status);

				if (isApproved)
				{
					_logger.LogInformation("Pagamento aprovado para OrderId={orderId}", request.OrderId);
					return Ok(new { message = "Pagamento aprovado com sucesso.", orderId = request.OrderId });
				}

				_logger.LogWarning("Pagamento recusado para OrderId={orderId}", request.OrderId);
				return BadRequest(new { message = "Pagamento recusado.", orderId = request.OrderId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro ao processar pagamento para OrderId={orderId}", request.OrderId);
				throw;
			}
		}

		[HttpGet("health")]
		[AllowAnonymous]
		public IActionResult Health()
		{
			return Ok(new { status = "healthy", service = "PaymentsAPI" });
		}
	}
}
