using System.ComponentModel;

namespace Payments.Api.Models
{
	public class PaymentRequest
	{
		[DefaultValue("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]
		public string OrderId { get; set; } = string.Empty;

		[DefaultValue("11111111-2222-3333-4444-555555555555")]
		public string UserId { get; set; } = string.Empty;

		[DefaultValue("aaaabbbb-cccc-dddd-eeee-ffffffffffff")]
		public string GameId { get; set; } = string.Empty;

		[DefaultValue("usuario@email.com")]
		public string EmailUser { get; set; } = string.Empty;

		[DefaultValue(59.99)]
		public decimal Price { get; set; }
	}
}
