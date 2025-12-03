using MassTransit;
using PaymentsAPI.Events;

var builder = WebApplication.CreateBuilder(args);

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPlacedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
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
