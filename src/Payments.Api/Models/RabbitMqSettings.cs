namespace Payments.Api.Models
{
	public class RabbitMqSettings
	{
		public string HostName { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		public int Port { get; set; }
		public string ExchangeName { get; set; }
		public string QueueNameOrderPlaced { get; set; }
		public string QueueNamePaymentProcessed { get; set; }
	}
}
