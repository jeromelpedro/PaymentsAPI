using MassTransit;
using Microsoft.Extensions.Options;
using Payments.Api.Consumers;
using Payments.Api.Models;

namespace Payments.Api.Configurations
{
	public static class MassTransitConfig
	{
		public static IServiceCollection AddServiceBusConfiguration(this IServiceCollection services, IConfiguration configuration)
		{
			services.Configure<ServiceBusSettings>(configuration.GetSection("ServiceBus"));

			services.AddMassTransit(x =>
			{
				x.AddConsumer<OrderPlacedConsumer>();

				x.UsingAzureServiceBus((context, cfg) =>
				{
					var settings = context.GetRequiredService<IOptions<ServiceBusSettings>>().Value;

					cfg.Host(settings.ConnectionString);

					cfg.Message<OrderPlacedEvent>(e => e.SetEntityName(settings.OrderPlacedTopicName));
					cfg.Message<PaymentProcessedEvent>(e => e.SetEntityName(settings.PaymentProcessedTopicName));

					// Subscription endpoint - assumes topic and subscription already exist
					cfg.SubscriptionEndpoint<OrderPlacedEvent>(settings.OrderPlacedSubscriptionName, e =>
					{
						// Disable automatic entity management (requires only Send/Listen permissions)
						e.ConfigureConsumeTopology = false;
						e.ConfigureConsumer<OrderPlacedConsumer>(context);
					});
				});
			});

			return services;
		}
	}
}
