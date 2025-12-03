namespace PaymentsAPI.Events
{
    public class OrderPlacedEvent
    {
        public Guid UserId { get; set; }
        public Guid GameId { get; set; }
        public decimal Price { get; set; }
    }

    public class PaymentProcessedEvent
    {
        public Guid UserId { get; set; }
        public Guid GameId { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; } // "Approved" or "Rejected"
    }
}