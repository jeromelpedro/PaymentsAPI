namespace Payments.Api.Models
{
	public class OrderPlacedEvent
	{
		public string OrderId { get; set; } = string.Empty;
		public string UserId { get; set; } = string.Empty;
		public string GameId { get; set; } = string.Empty;
		public decimal Price { get; set; }
	}
}
