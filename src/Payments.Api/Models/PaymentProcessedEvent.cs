namespace Payments.Api.Models
{
	public class PaymentProcessedEvent
	{
		public required string OrderId { get; set; }
		public required string Status { get; set; } // "Approved", "Rejected"
	}
}
