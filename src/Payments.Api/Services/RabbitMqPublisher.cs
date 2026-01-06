using Microsoft.Extensions.Options;
using Payments.Api.Models;
using Payments.Api.Services.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Payments.Api.Services
{
	public class RabbitMqPublisher : IRabbitMqPublisher
	{
		private readonly RabbitMqSettings _settings;
		private readonly JsonSerializerOptions _jsonOptions;

		public RabbitMqPublisher(IOptions<RabbitMqSettings> options)
		{
			_settings = options.Value;
			_jsonOptions = new JsonSerializerOptions
			{
				Converters = { new JsonStringEnumConverter() }
			};
		}

		public async Task PublishAsync<T>(T message, string topic)
		{
			var json = JsonSerializer.Serialize(message, _jsonOptions);
			var body = Encoding.UTF8.GetBytes(json);

			var factory = new ConnectionFactory
			{
				HostName = _settings.HostName,
				Port = _settings.Port,
				UserName = _settings.UserName,
				Password = _settings.Password,
			};

			using (var connection = await factory.CreateConnectionAsync())
			using (var channel = await connection.CreateChannelAsync())
			{				
				await channel.ExchangeDeclareAsync(
					exchange: _settings.ExchangeName,
					type: ExchangeType.Topic,
					durable: true,
					autoDelete: false,
					arguments: null);

				await channel.QueueDeclareAsync(
					queue: topic,
					durable: true,
					exclusive: false,
					autoDelete: false,
					arguments: null);

				await channel.QueueBindAsync(
					queue: topic,
					exchange: _settings.ExchangeName,
					routingKey: topic,
					arguments: null);

				var properties = new BasicProperties
				{
					Persistent = true,
					ContentType = "application/json"
				};

				await channel.BasicPublishAsync(
					exchange: _settings.ExchangeName,
					routingKey: topic,
					mandatory: false,
					basicProperties: properties,
					body: body);
			}			
		}
	}
}
