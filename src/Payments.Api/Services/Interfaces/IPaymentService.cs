namespace Payments.Api.Services.Interfaces
{
	public interface IPaymentService
	{
		Task<bool> ProcessPaymentAsync(Guid orderId, Guid userId, decimal amount);
		Task<bool> ProcessPaymentAsync(Guid orderId, Guid userId, string userEmail, Guid gameId, decimal amount);
	}
}
