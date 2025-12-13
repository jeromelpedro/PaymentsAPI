using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Payments.Api.Models;
using Payments.Api.Services.Interfaces;

namespace Payments.Api.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PaymentsController : ControllerBase
	{
		private readonly IPaymentService _paymentService;
		private readonly IPublishEndpoint _publishEndpoint;

		public PaymentsController(IPaymentService paymentService, IPublishEndpoint publishEndpoint)
		{
			_paymentService = paymentService;
			_publishEndpoint = publishEndpoint;
		}

		[HttpPost("process")]
		public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
		{
			var isApproved = await _paymentService.ProcessPaymentAsync(
				request.OrderId, 
				request.UserId, 
				request.UserEmail, 
				request.GameId, 
				request.Amount);

			var paymentEvent = new PaymentProcessedEvent
			{
				OrderId = request.OrderId,
				UserId = request.UserId,
				UserEmail = request.UserEmail,
				Amount = request.Amount,
				Status = isApproved ? "Approved" : "Declined"
			};

			await _publishEndpoint.Publish(paymentEvent);

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
