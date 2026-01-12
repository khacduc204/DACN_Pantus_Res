namespace KD_Restaurant.Options
{
    public class MomoOptions
    {
        public string PartnerCode { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";
        public string RequestType { get; set; } = "captureWallet";
        public string Lang { get; set; } = "vi";
        public string PartnerName { get; set; } = "MoMo Payment";
        public string StoreId { get; set; } = "Test Store";
        public string OrderGroupId { get; set; } = string.Empty;
        public bool AutoCapture { get; set; } = true;
    }
}
