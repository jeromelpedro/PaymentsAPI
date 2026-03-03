using Microsoft.Extensions.Options;
using Payments.Api.Models;
using Payments.Api.Services.Interfaces;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Payments.Api.Services
{
	public class RabbitMqPublisher : IRabbitMqPublisher
	{
		private readonly RabbitMqSettings _settings;
		private readonly ILogger<RabbitMqPublisher> _logger;

		public RabbitMqPublisher(IOptions<RabbitMqSettings> options, ILogger<RabbitMqPublisher> logger)
		{
			_settings = options.Value;
			_logger = logger;
		}

		public async Task PublishAsync<T>(T message, string topic)
		{
			_logger.LogInformation("PublishAsync iniciado para topic={topic}", topic);

			try
			{
				var json = JsonSerializer.Serialize(message);
				var body = Encoding.UTF8.GetBytes(json);

				_logger.LogDebug("Mensagem serializada: {message}", json);

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

					_logger.LogInformation("Mensagem publicada com sucesso no topic={topic} exchange={exchange}",
						topic, _settings.ExchangeName);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Erro ao publicar mensagem no topic={topic}", topic);
				throw;
			}
		}
	}
}
