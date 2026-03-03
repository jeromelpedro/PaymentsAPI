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

		public Task<bool> ProcessPaymentAsync(string orderId, string userId, string gameId, decimal price)
		{
			_logger.LogInformation("ProcessPaymentAsync iniciado para OrderId={orderId}", orderId);

			try
			{
				_logger.LogInformation("--------------------------------------------------");
				_logger.LogInformation("[PAYMENT SERVICE] Processing payment");
				_logger.LogInformation("[ORDER ID] {OrderId}", orderId);
				_logger.LogInformation("[USER ID] {UserId}", userId);
				_logger.LogInformation("[GAME ID] {GameId}", gameId);
				_logger.LogInformation("[PRICE] R$ {Price}", price);

				// Simulate payment processing logic
				var isApproved = SimulatePaymentGateway(price);

				_logger.LogInformation("[STATUS] {Status}", isApproved ? "APPROVED" : "DECLINED");
				_logger.LogInformation("--------------------------------------------------");

				_logger.LogInformation("ProcessPaymentAsync concluído para OrderId={orderId} Status={status}",
					orderId, isApproved ? "APPROVED" : "DECLINED");

				return Task.FromResult(isApproved);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro em ProcessPaymentAsync para OrderId={orderId}", orderId);
				throw;
			}
		}

		private bool SimulatePaymentGateway(decimal price)
		{
			// Simple simulation: approve all payments under 10000
			// In production, this would call an actual payment gateway
			return price < 10000;
		}
	}
}
