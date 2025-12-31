using Payments.Api.Configurations;
using Payments.Api.Services;
using Payments.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddRabbitMqConfiguration(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<IPaymentService, PaymentService>();
builder.Services.AddTransient<IRabbitMqPublisher, RabbitMqPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => "PaymentsAPI is running on port 5055...");

await app.RunAsync();
