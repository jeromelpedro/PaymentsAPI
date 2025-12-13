namespace Payments.Api.Models
{
	public class PaymentRequest
	{
		public Guid OrderId { get; set; }
		public Guid UserId { get; set; }
		public string UserEmail { get; set; }
		public Guid GameId { get; set; }
		public decimal Amount { get; set; }
	}
}
