using Serilog;
using OpenTelemetry.Trace;
using Payments.Api.Configurations;
using Payments.Api.Services;
using Payments.Api.Services.Interfaces;
using Payments.Api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Information()
	.Enrich.FromLogContext()
	.Enrich.With(new Payments.Api.Serilog.ActivityEnricher())
	.WriteTo.Console()
	.CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddServiceBusConfiguration(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthConfiguration(builder.Configuration);

// Application Insights (reads CONNECTION STRING from APPLICATIONINSIGHTS_CONNECTION_STRING env var)
builder.Services.AddApplicationInsightsTelemetry();

// Http context accessor
builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<IPaymentService, PaymentService>();
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
