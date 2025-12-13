namespace Payments.Api.Models
{
	public class PaymentProcessedEvent
	{
		public Guid OrderId { get; set; }
		public Guid UserId { get; set; }
		public string UserEmail { get; set; }
		public decimal Amount { get; set; }
		public string Status { get; set; } // "Approved", "Declined"
	}
}
