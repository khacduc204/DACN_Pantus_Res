using System.Text.Json.Serialization;

namespace KD_Restaurant.Services.Models
{
    public class MomoPaymentRequest
    {
        public string OrderId { get; set; } = string.Empty;
        public string OrderInfo { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string ReturnUrl { get; set; } = string.Empty;
        public string NotifyUrl { get; set; } = string.Empty;
        public string ExtraData { get; set; } = string.Empty;
    }

    public class MomoPaymentResult
    {
        public bool Success { get; }
        public string? PayUrl { get; }
        public string? Message { get; }

        public MomoPaymentResult(bool success, string? payUrl, string? message)
        {
            Success = success;
            PayUrl = payUrl;
            Message = message;
        }
    }

    internal class MomoCreatePaymentResponse
    {
        [JsonPropertyName("resultCode")]
        public int ResultCode { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("payUrl")]
        public string? PayUrl { get; set; }

        [JsonPropertyName("deeplink")]
        public string? Deeplink { get; set; }

        [JsonPropertyName("qrCodeUrl")]
        public string? QrCodeUrl { get; set; }
    }
}
