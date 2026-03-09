namespace Payments.Api.Models
{
	public class ServiceBusSettings
	{
		public string ConnectionString { get; set; } = string.Empty;
		public string OrderPlacedTopicName { get; set; } = "order-placed";
		public string OrderPlacedSubscriptionName { get; set; } = "payments-api";
		public string PaymentProcessedTopicName { get; set; } = "payment-processed";
	}
}