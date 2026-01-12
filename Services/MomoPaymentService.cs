using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using KD_Restaurant.Options;
using KD_Restaurant.Services.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KD_Restaurant.Services
{
    public class MomoPaymentService : IMomoPaymentService
    {
        private readonly HttpClient _httpClient;
        private readonly MomoOptions _options;
        private readonly ILogger<MomoPaymentService> _logger;

        public MomoPaymentService(HttpClient httpClient, IOptions<MomoOptions> options, ILogger<MomoPaymentService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<MomoPaymentResult> CreatePaymentAsync(MomoPaymentRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.PartnerCode) || string.IsNullOrWhiteSpace(_options.AccessKey) || string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                return new MomoPaymentResult(false, null, "Chưa cấu hình thông tin MoMo.");
            }

            var requestId = Guid.NewGuid().ToString("N");
            var rawSignature =
                $"accessKey={_options.AccessKey}" +
                $"&amount={request.Amount}" +
                $"&extraData={request.ExtraData}" +
                $"&ipnUrl={request.NotifyUrl}" +
                $"&orderId={request.OrderId}" +
                $"&orderInfo={request.OrderInfo}" +
                $"&partnerCode={_options.PartnerCode}" +
                $"&redirectUrl={request.ReturnUrl}" +
                $"&requestId={requestId}" +
                $"&requestType={_options.RequestType}";

            var signature = Sign(rawSignature, _options.SecretKey);

            var payload = new
            {
                partnerCode = _options.PartnerCode,
                partnerName = string.IsNullOrWhiteSpace(_options.PartnerName) ? "KD Restaurant" : _options.PartnerName,
                storeId = string.IsNullOrWhiteSpace(_options.StoreId) ? "KD_Restaurant" : _options.StoreId,
                requestId,
                amount = request.Amount.ToString(),
                orderId = request.OrderId,
                orderInfo = request.OrderInfo,
                redirectUrl = request.ReturnUrl,
                ipnUrl = request.NotifyUrl,
                lang = _options.Lang,
                requestType = _options.RequestType,
                extraData = request.ExtraData,
                orderGroupId = _options.OrderGroupId ?? string.Empty,
                autoCapture = _options.AutoCapture,
                signature
            };

            var json = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync(_options.Endpoint, httpContent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tạo yêu cầu thanh toán MoMo");
                return new MomoPaymentResult(false, null, "Không thể kết nối MoMo. Vui lòng thử lại sau.");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MoMo API trả về lỗi: {Status} - {Body}", response.StatusCode, responseBody);
                return new MomoPaymentResult(false, null, "MoMo tạm thời không khả dụng. Vui lòng thử lại.");
            }

            MomoCreatePaymentResponse? momoResponse;
            try
            {
                momoResponse = JsonSerializer.Deserialize<MomoCreatePaymentResponse>(responseBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể phân tích phản hồi MoMo: {Body}", responseBody);
                return new MomoPaymentResult(false, null, "Không đọc được phản hồi từ MoMo.");
            }

            if (momoResponse == null || momoResponse.ResultCode != 0 || string.IsNullOrWhiteSpace(momoResponse.PayUrl))
            {
                var message = momoResponse?.Message ?? "Không tạo được liên kết thanh toán.";
                _logger.LogWarning("MoMo tạo đơn thất bại: {Message}", message);
                return new MomoPaymentResult(false, null, message);
            }

            return new MomoPaymentResult(true, momoResponse.PayUrl, momoResponse.Message);
        }

        private static string Sign(string rawData, string secretKey)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            var builder = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
            {
                builder.AppendFormat("{0:x2}", b);
            }
            return builder.ToString();
        }
    }
}
