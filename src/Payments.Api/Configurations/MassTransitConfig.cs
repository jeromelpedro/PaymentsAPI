using MassTransit;
using Microsoft.Extensions.Options;
using Payments.Api.Consumers;
using Payments.Api.Models;

namespace Payments.Api.Configurations
{
	public static class MassTransitConfig
	{
		public static IServiceCollection AddRabbitMqConfiguration(this IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<RabbitMqSettings>(configuration.GetSection("RabbitMq"));

			services.AddMassTransit(x =>
			{
				x.AddConsumer<OrderPlacedConsumer>();

				x.UsingRabbitMq((context, cfg) =>
				{
					var settings = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;

					var uri = new Uri($"rabbitmq://{settings.HostName}:{settings.Port}/");

					cfg.Host(uri, h =>
					{
						h.Username(settings.UserName);
						h.Password(settings.Password);
					});

					cfg.Publish<OrderPlacedEvent>(p => p.Exclude = true);

					//cfg.Message<PaymentProcessedEvent>(e => e.SetEntityName(settings.ExchangeName));
					//cfg.Publish<PaymentProcessedEvent>(p =>
					//{
					//	p.ExchangeType = "topic";
					//});
					//cfg.Send<PaymentProcessedEvent>(s =>
					//{
					//	s.UseRoutingKeyFormatter(ctx => settings.QueueNamePaymentProcessed);
					//});

					cfg.ReceiveEndpoint(settings.QueueNameOrderPlaced, e =>
					{
						e.ConfigureConsumeTopology = false;
						e.Durable = true;
						e.AutoDelete = false;

						e.ClearSerialization();
						e.UseRawJsonSerializer();

						e.ConfigureConsumer<OrderPlacedConsumer>(context);

						e.Bind(settings.ExchangeName, s =>
						{
							s.RoutingKey = settings.QueueNameOrderPlaced;
							s.ExchangeType = "topic";
						});
					});
				});
			});

			return services;
		}
	}
}
