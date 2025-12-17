namespace Payments.Api.Services.Interfaces
{
	public interface IPaymentService
	{
		Task<bool> ProcessPaymentAsync(string orderId, string userId, string gameId, decimal price);
	}
}
