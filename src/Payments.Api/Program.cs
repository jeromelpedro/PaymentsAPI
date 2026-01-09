using Payments.Api.Configurations;
using Payments.Api.Services;
using Payments.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddRabbitMqConfiguration(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthConfiguration(builder.Configuration);


builder.Services.AddTransient<IPaymentService, PaymentService>();
builder.Services.AddTransient<IRabbitMqPublisher, RabbitMqPublisher>();
builder.Services.AddSwaggerConfiguration();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
	app.UseSwagger();
	app.UseSwaggerUI();
//}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => "PaymentsAPI is running...");

await app.RunAsync();
