namespace Payments.Api.Services.Interfaces
{
	public interface IRabbitMqPublisher
	{
		Task PublishAsync<T>(T message, string queueName);
	}
}
