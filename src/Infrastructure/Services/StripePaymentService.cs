namespace BeautyEcommerce.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

/// <summary>
/// Service tích hợp Stripe payment gateway
/// </summary>
public class StripePaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly string _apiKey;
    private readonly string _webhookSecret;

    public StripePaymentService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<StripePaymentService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _apiKey = config["Stripe:SecretKey"] ?? "";
        _webhookSecret = config["Stripe:WebhookSecret"] ?? "";
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<(string PaymentUrl, string PaymentIntentId)> CreatePaymentIntentAsync(
        Guid orderId, decimal amount, CancellationToken cancellationToken)
    {
        try
        {
            // Chuyển đổi sang cents (Stripe dùng đơn vị nhỏ nhất của tiền tệ)
            var amountInCents = (long)(amount * 100);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("amount", amountInCents.ToString()),
                new KeyValuePair<string, string>("currency", "vnd"),
                new KeyValuePair<string, string>("metadata[orderId]", orderId.ToString()),
                new KeyValuePair<string, string>("automatic_payment_methods[enabled]", "true")
            });

            var response = await _httpClient.PostAsync("https://api.stripe.com/v1/payment_intents", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<StripePaymentIntentResponse>(cancellationToken: cancellationToken);
            
            if (result == null || string.IsNullOrEmpty(result.ClientSecret))
                throw new InvalidOperationException("Failed to create payment intent");

            // Payment URL sẽ là frontend URL với client_secret
            var frontendUrl = _config["FrontendUrl"] ?? "http://localhost:3000";
            var paymentUrl = $"{frontendUrl}/checkout?payment_intent_client_secret={result.ClientSecret}&order_id={orderId}";

            return (paymentUrl, result.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Stripe payment intent for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<(string PaymentUrl, string TransactionId)> CreatePayOSLinkAsync(
        Guid orderId, decimal amount, CancellationToken cancellationToken)
    {
        try
        {
            var clientId = _config["PayOS:ClientId"] ?? "";
            var apiKey = _config["PayOS:ApiKey"] ?? "";
            var checksumKey = _config["PayOS:ChecksumKey"] ?? "";

            // Tạo signature
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var dataToSign = $"{clientId}{apiKey}{orderId}{amount}{timestamp}";
            var signature = GenerateHmacSha256(dataToSign, checksumKey);

            var payload = new
            {
                orderCode = orderId.ToString("N").Substring(0, 16), // PayOS yêu cầu max 16 ký tự
                amount = (long)amount,
                description = $"Thanh toán đơn hàng {orderId}",
                cancelUrl = $"{_config["FrontendUrl"]}/order/cancel?orderId={orderId}",
                returnUrl = $"{_config["FrontendUrl"]}/order/success?orderId={orderId}",
                signature = signature,
                expiredAt = timestamp + 3600 // 1 hour
            };

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-client-id", clientId);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

            var response = await _httpClient.PostAsync("https://api-merchant.payos.vn/v2/payment-requests", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<PayOSResponse>(cancellationToken: cancellationToken);

            if (result?.Data?.CheckoutUrl == null)
                throw new InvalidOperationException("Failed to create PayOS payment link");

            return (result.Data.CheckoutUrl, result.Data.QrCode ?? result.Data.CheckoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create PayOS payment link for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task ProcessPaymentAsync(Guid orderId, CancellationToken cancellationToken)
    {
        // Implementation sẽ được gọi từ webhook khi thanh toán thành công
        await Task.CompletedTask;
    }

    public async Task RefundPaymentAsync(Guid sagaStateId, CancellationToken cancellationToken)
    {
        // Implement refund logic qua Stripe/PayOS API
        _logger.LogInformation("Refund payment for saga {SagaStateId}", sagaStateId);
        await Task.CompletedTask;
    }

    private static string GenerateHmacSha256(string message, string secret)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}

// Stripe API Response models
public class StripePaymentIntentResponse
{
    public string Id { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

// PayOS API Response models
public class PayOSResponse
{
    public int Code { get; set; }
    public string Desc { get; set; } = string.Empty;
    public PayOSData? Data { get; set; }
}

public class PayOSData
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
}

/// <summary>
/// Interface cho Payment Service
/// </summary>
public interface IPaymentService
{
    Task<(string PaymentUrl, string PaymentIntentId)> CreatePaymentIntentAsync(Guid orderId, decimal amount, CancellationToken cancellationToken);
    Task<(string PaymentUrl, string TransactionId)> CreatePayOSLinkAsync(Guid orderId, decimal amount, CancellationToken cancellationToken);
    Task ProcessPaymentAsync(Guid orderId, CancellationToken cancellationToken);
    Task RefundPaymentAsync(Guid sagaStateId, CancellationToken cancellationToken);
}
