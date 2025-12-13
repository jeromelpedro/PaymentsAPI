using Payments.Api.Services.Interfaces;

namespace Payments.Api.Services
{
	public class PaymentService : IPaymentService
	{
		private readonly ILogger<PaymentService> _logger;

		public PaymentService(ILogger<PaymentService> logger)
		{
			_logger = logger;
		}

		public Task<bool> ProcessPaymentAsync(Guid orderId, Guid userId, decimal amount)
		{
			_logger.LogInformation("--------------------------------------------------");
			_logger.LogInformation("[PAYMENT SERVICE] Processing payment");
			_logger.LogInformation("[ORDER ID] {OrderId}", orderId);
			_logger.LogInformation("[USER ID] {UserId}", userId);
			_logger.LogInformation("[AMOUNT] R$ {Amount}", amount);

			// Simulate payment processing logic
			// In a real scenario, this would integrate with a payment gateway
			var isApproved = SimulatePaymentGateway(amount);

			_logger.LogInformation("[STATUS] {Status}", isApproved ? "APPROVED" : "DECLINED");
			_logger.LogInformation("--------------------------------------------------");

			return Task.FromResult(isApproved);
		}

		public Task<bool> ProcessPaymentAsync(Guid orderId, Guid userId, string userEmail, Guid gameId, decimal amount)
		{
			_logger.LogInformation("--------------------------------------------------");
			_logger.LogInformation("[PAYMENT SERVICE] Processing payment");
			_logger.LogInformation("[ORDER ID] {OrderId}", orderId);
			_logger.LogInformation("[USER ID] {UserId}", userId);
			_logger.LogInformation("[USER EMAIL] {UserEmail}", userEmail);
			_logger.LogInformation("[GAME ID] {GameId}", gameId);
			_logger.LogInformation("[AMOUNT] R$ {Amount}", amount);

			// Simulate payment processing logic
			var isApproved = SimulatePaymentGateway(amount);

			_logger.LogInformation("[STATUS] {Status}", isApproved ? "APPROVED" : "DECLINED");
			_logger.LogInformation("--------------------------------------------------");

			return Task.FromResult(isApproved);
		}

		private bool SimulatePaymentGateway(decimal amount)
		{
			// Simple simulation: approve all payments under 10000
			// In production, this would call an actual payment gateway
			return amount < 10000;
		}
	}
}
