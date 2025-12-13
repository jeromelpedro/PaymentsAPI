using MassTransit;
using PaymentsAPI.Events;

var builder = WebApplication.CreateBuilder(args);

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        var host = Environment.GetEnvironmentVariable("RabbitMq__HostName") ?? "rabbitmq";
        var port = ushort.Parse(Environment.GetEnvironmentVariable("RabbitMq__Port") ?? "5672");
        var username = Environment.GetEnvironmentVariable("RabbitMq__UserName") ?? "guest";
        var password = Environment.GetEnvironmentVariable("RabbitMq__Password") ?? "guest";
        var exchangeName = Environment.GetEnvironmentVariable("RabbitMq__ExchangeName") ?? "cloudgames.topic";
        
        cfg.Host(host, port, "/", h =>
        {
            h.Username(username);
            h.Password(password);
        });
        
        cfg.Message<PaymentProcessedEvent>(x => x.SetEntityName(exchangeName));
        
        cfg.ReceiveEndpoint("order-placed-queue", e =>
        {
            e.ConfigureConsumer<OrderPlacedConsumer>(context);
        });
    });
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

// Consumer implementation
public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        // Simulate payment processing
        var approved = true; // Here you can add your payment logic
        var paymentEvent = new PaymentProcessedEvent
        {
            UserId = context.Message.UserId,
            GameId = context.Message.GameId,
            Price = context.Message.Price,
            Status = approved ? "Approved" : "Rejected"
        };
        await context.Publish(paymentEvent);
    }
}
