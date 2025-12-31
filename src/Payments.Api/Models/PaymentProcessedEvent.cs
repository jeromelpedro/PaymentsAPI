namespace Payments.Api.Models
{
	public enum PaymentStatus
	{
		Approved,
		Rejected
	}
	public class PaymentProcessedEvent
	{
		public required string OrderId { get; set; }
		public required decimal Price { get; set; }
		public required string UserId { get; set; } = string.Empty;
		public required string GameId { get; set; } = string.Empty;
		public required string EmailUser { get; set; } = string.Empty;
		public required PaymentStatus Status { get; set; } // "Approved", "Rejected"
	}
}
