namespace Payments.Api.Models
{
	public class OrderPlacedEvent
	{
		public Guid OrderId { get; set; }
		public Guid UserId { get; set; }
		public string UserEmail { get; set; }
		public Guid GameId { get; set; }
		public decimal Price { get; set; }
	}
}
