using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights.Extensibility;
using OpenTelemetry.Trace;
using Payments.Api.Configurations;
using Payments.Api.Consumers;
using Payments.Api.Middlewares;
using Payments.Api.Services;
using Payments.Api.Services.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddApplicationInsightsTelemetry();

builder.Host.UseSerilog((_, services, loggerConfiguration) => loggerConfiguration
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.Enrich.With(new Payments.Api.Serilog.ActivityEnricher())
	.WriteTo.Console()
	.WriteTo.ApplicationInsights(
		services.GetRequiredService<TelemetryConfiguration>(),
		TelemetryConverter.Traces));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthConfiguration(builder.Configuration);

// ServiceBus
builder.Services.AddSingleton<IServiceBus, ServiceBus>();
builder.Services.AddSingleton<ServiceBusClient>(provider =>
{
	var connectionString = builder.Configuration["ServiceBus:ConnectionString"];
	return new ServiceBusClient(connectionString);
});
builder.Services.AddHostedService<ServiceBusConsumer>();

// Http context accessor
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<IPaymentService, PaymentService>();
builder.Services.AddTransient<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddSwaggerConfiguration();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
	app.UseSwagger();
	app.UseSwaggerUI();
//}

app.UseErrorHandling();
app.UseCorrelationId();
app.UseRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => "PaymentsAPI is running...");

await app.RunAsync();
