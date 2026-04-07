namespace BeautyEcommerce.Infrastructure.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using BeautyEcommerce.Infrastructure.Persistence;

/// <summary>
/// Service tích hợp Giao Hàng Nhanh (GHN) cho vận chuyển
/// </summary>
public class GhnShippingService : IShipmentService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<GhnShippingService> _logger;
    private readonly AppDbContext _dbContext;
    private readonly string _shopId;
    private readonly string _token;

    public GhnShippingService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<GhnShippingService> logger,
        AppDbContext dbContext)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _dbContext = dbContext;
        _shopId = config["GHN:ShopId"] ?? "";
        _token = config["GHN:Token"] ?? "";
        
        _httpClient.DefaultRequestHeaders.Add("Token", _token);
        _httpClient.DefaultRequestHeaders.Add("ShopId", _shopId);
    }

    public async Task CreateShipmentAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
            throw new InvalidOperationException($"Order {orderId} not found");

        try
        {
            // 1. Tìm district và ward code từ GHN API
            var locationInfo = await GetLocationCodesAsync(order.ShippingCity, order.ShippingDistrict, order.ShippingWard, cancellationToken);

            // 2. Tính shipping fee
            var shippingFee = await CalculateShippingFeeAsync(locationInfo.DistrictId, order.Total, cancellationToken);

            // 3. Tạo đơn hàng trên GHN
            var ghnOrder = new
            {
                payment_type_id = order.PaymentMethod == PaymentMethod.COD ? 2 : 1, // 1=Prepaid, 2=COD
                required_note = "CHOXEMHANGTHU",
                from_name = _config["Store:Name"] ?? "Beauty Ecommerce",
                from_phone = _config["Store:Phone"] ?? "0901234567",
                from_address = _config["Store:Address"] ?? "Hà Nội",
                from_ward_name = "",
                from_district_name = "",
                from_city_name = _config["Store:City"] ?? "Hà Nội",
                to_name = order.CustomerName,
                to_phone = order.CustomerPhone,
                to_address = $"{order.ShippingAddressLine1}, {order.ShippingAddressLine2}",
                to_ward_code = locationInfo.WardCode,
                to_district_id = locationInfo.DistrictId,
                to_city_id = locationInfo.CityId,
                weight = 200, // Tạm tính 200g, sẽ tính từ sản phẩm thực tế
                length = 10,
                width = 10,
                height = 10,
                service_id = 0, // Để GHN tự chọn service
                service_type_id = 2, // Tiêu chuẩn
                cod_amount = order.PaymentMethod == PaymentMethod.COD ? (long)order.Total : 0,
                content = $"Đơn hàng {order.OrderNumber}",
                items = order.Items.Select(i => new
                {
                    name = i.ProductName,
                    code = i.ProductSku,
                    quantity = i.Quantity,
                    price = (long)i.UnitPrice,
                    length = 5,
                    width = 5,
                    height = 5,
                    weight = 50
                }).ToList(),
                note = order.CustomerNote
            };

            var jsonContent = JsonSerializer.Serialize(ghnOrder);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://dev-online-gateway.ghn.vn/shiip/public-api/v2/shipping-order/create", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GhnCreateOrderResponse>(cancellationToken: cancellationToken);

            if (result?.Data?.OrderCode == null)
                throw new InvalidOperationException("Failed to create GHN shipment");

            // 4. Lưu shipment info vào database
            var shipment = new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                Status = ShipmentStatus.Pending,
                CarrierCode = "GHN",
                TrackingNumber = result.Data.TrackingCode,
                ShippingFee = shippingFee,
                ExternalShipmentId = result.Data.OrderCode.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            order.ShipmentId = shipment.Id;
            order.CarrierCode = "GHN";
            order.TrackingNumber = result.Data.TrackingCode;

            _dbContext.Shipments.Add(shipment);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created GHN shipment {TrackingCode} for order {OrderId}", 
                result.Data.TrackingCode, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create GHN shipment for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task CancelShipmentAsync(Guid sagaStateId, CancellationToken cancellationToken)
    {
        // Implement cancel shipment logic với GHN API
        _logger.LogInformation("Cancel shipment for saga {SagaStateId}", sagaStateId);
        await Task.CompletedTask;
    }

    public async Task UpdateShipmentStatusAsync(string trackingCode, string status, CancellationToken cancellationToken)
    {
        // Method này sẽ được gọi từ webhook khi GHN cập nhật trạng thái
        var shipment = await _dbContext.Shipments
            .Include(s => s.Order)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingCode, cancellationToken);

        if (shipment == null)
            return;

        var orderStatus = status switch
        {
            "picked" => OrderStatus.Processing,
            "delivering" => OrderStatus.Delivering,
            "delivered" => OrderStatus.Delivered,
            "return" => OrderStatus.Cancelled,
            _ => shipment.Order.Status
        };

        shipment.ExternalStatus = status;
        shipment.Order.Status = orderStatus;

        if (status == "delivered")
        {
            shipment.DeliveredAt = DateTime.UtcNow;
            shipment.Status = ShipmentStatus.Delivered;
            shipment.Order.DeliveredAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<(int CityId, int DistrictId, string WardCode)> GetLocationCodesAsync(
        string city, string district, string ward, CancellationToken cancellationToken)
    {
        // Gọi API GHN để lấy location codes
        // Đây là implementation mẫu, trong thực tế cần cache lại để giảm API calls
        
        // Get cities
        var citiesResponse = await _httpClient.GetAsync("https://dev-online-gateway.ghn.vn/shiip/public-api/master-data/province", cancellationToken);
        var cities = await citiesResponse.Content.ReadFromJsonAsync<List<GhnProvince>>(cancellationToken: cancellationToken);
        
        var cityMatch = cities?.FirstOrDefault(c => c.ProvinceName.Contains(city, StringComparison.OrdinalIgnoreCase));
        if (cityMatch == null)
            throw new InvalidOperationException($"Không tìm thấy tỉnh/thành phố: {city}");

        // Get districts
        var districtsResponse = await _httpClient.GetAsync(
            $"https://dev-online-gateway.ghn.vn/shiip/public-api/master-data/district?province_id={cityMatch.ProvinceId}", 
            cancellationToken);
        var districts = await districtsResponse.Content.ReadFromJsonAsync<List<GhnDistrict>>(cancellationToken: cancellationToken);
        
        var districtMatch = districts?.FirstOrDefault(d => d.DistrictName.Contains(district, StringComparison.OrdinalIgnoreCase));
        if (districtMatch == null)
            throw new InvalidOperationException($"Không tìm thấy quận/huyện: {district}");

        // Get wards
        var wardsResponse = await _httpClient.GetAsync(
            $"https://dev-online-gateway.ghn.vn/shiip/public-api/master-data/ward?district_id={districtMatch.DistrictId}", 
            cancellationToken);
        var wards = await wardsResponse.Content.ReadFromJsonAsync<List<GhnWard>>(cancellationToken: cancellationToken);
        
        var wardMatch = wards?.FirstOrDefault(w => w.WardName.Contains(ward, StringComparison.OrdinalIgnoreCase));
        if (wardMatch == null)
            throw new InvalidOperationException($"Không tìm thấy phường/xã: {ward}");

        return (cityMatch.ProvinceId, districtMatch.DistrictId, wardMatch.WardCode);
    }

    private async Task<decimal> CalculateShippingFeeAsync(int districtId, decimal orderValue, CancellationToken cancellationToken)
    {
        // Gọi API tính phí của GHN
        var feeRequest = new
        {
            service_id = 0,
            service_type_id = 2,
            to_district_id = districtId,
            weight = 200,
            value = (long)orderValue
        };

        var jsonContent = JsonSerializer.Serialize(feeRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            "https://dev-online-gateway.ghn.vn/shiip/public-api/v2/shipping-order/fee", 
            content, cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        var result = await response.Content.ReadFromJsonAsync<GhnFeeResponse>(cancellationToken: cancellationToken);
        
        return result?.Data?.Total ?? 30000m; // Default 30k nếu không tính được
    }
}

// GHN API Response models
public class GhnCreateOrderResponse
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public GhnOrderData? Data { get; set; }
}

public class GhnOrderData
{
    public long OrderCode { get; set; }
    public string TrackingCode { get; set; } = string.Empty;
}

public class GhnFeeResponse
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public GhnFeeData? Data { get; set; }
}

public class GhnFeeData
{
    public long Total { get; set; }
}

public class GhnProvince
{
    public int ProvinceId { get; set; }
    public string ProvinceName { get; set; } = string.Empty;
}

public class GhnDistrict
{
    public int DistrictId { get; set; }
    public string DistrictName { get; set; } = string.Empty;
}

public class GhnWard
{
    public string WardCode { get; set; } = string.Empty;
    public string WardName { get; set; } = string.Empty;
}

/// <summary>
/// Interface cho Shipment Service
/// </summary>
public interface IShipmentService
{
    Task CreateShipmentAsync(Guid orderId, CancellationToken cancellationToken);
    Task CancelShipmentAsync(Guid sagaStateId, CancellationToken cancellationToken);
}
