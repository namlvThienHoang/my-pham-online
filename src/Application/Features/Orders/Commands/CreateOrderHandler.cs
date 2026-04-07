namespace BeautyEcommerce.Application.Features.Orders.Commands;

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BeautyEcommerce.Infrastructure.Persistence;
using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Enums;
using System.Transactions;

/// <summary>
/// Handler xử lý tạo đơn hàng với Saga pattern
/// </summary>
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, CreateOrderResult>
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly OrderSaga _orderSaga;
    private readonly IPaymentService _paymentService;

    public CreateOrderCommandHandler(
        AppDbContext dbContext,
        ILogger<CreateOrderCommandHandler> logger,
        OrderSaga orderSaga,
        IPaymentService paymentService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _orderSaga = orderSaga;
        _paymentService = paymentService;
    }

    public async Task<CreateOrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // 1. Lấy thông tin user và cart
            var user = await _dbContext.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
            if (user == null)
                throw new InvalidOperationException("User không tồn tại");

            var cart = await _dbContext.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .Include(c => c.Items)
                    .ThenInclude(i => i.Variant)
                .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

            if (cart == null || !cart.Items.Any())
                throw new InvalidOperationException("Giỏ hàng trống");

            // 2. Tính toán tổng tiền
            var subtotal = cart.Items.Sum(i => i.TotalPrice);
            var discountAmount = cart.DiscountAmount;
            
            // Áp dụng voucher nếu có
            decimal voucherDiscount = 0;
            string? appliedVoucherCode = null;
            if (!string.IsNullOrEmpty(request.VoucherCode))
            {
                var voucherResult = await ApplyVoucherAsync(request.VoucherCode, subtotal, request.UserId, cancellationToken);
                voucherDiscount = voucherResult.DiscountAmount;
                appliedVoucherCode = voucherResult.VoucherCode;
            }

            // Tính shipping fee (tạm tính cố định, sẽ tích hợp GHN/GHTK sau)
            decimal shippingFee = CalculateShippingFee(request.ShippingCity, subtotal);

            // Tổng cộng
            var totalAmount = subtotal - discountAmount - voucherDiscount + shippingFee;

            // 3. Tạo OrderNumber
            var orderNumber = GenerateOrderNumber();

            // 4. Tạo Order entity
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = orderNumber,
                UserId = request.UserId,
                Status = request.PaymentMethod == "COD" ? OrderStatus.Pending : OrderStatus.PaymentPending,
                PaymentStatus = PaymentStatus.Pending,
                Subtotal = subtotal,
                DiscountAmount = discountAmount + voucherDiscount,
                ShippingAmount = shippingFee,
                TaxAmount = 0, // Có thể thêm VAT sau
                Total = totalAmount,
                CurrencyCode = "VND",
                PaymentMethod = ParsePaymentMethod(request.PaymentMethod),
                CustomerNote = request.CustomerNote,
                
                // Customer info snapshot
                CustomerEmail = user.Email,
                CustomerName = user.FullName,
                CustomerPhone = user.PhoneNumber ?? string.Empty,
                
                // Shipping address
                ShippingAddressLine1 = request.ShippingAddressLine1,
                ShippingAddressLine2 = request.ShippingAddressLine2,
                ShippingWard = request.ShippingWard,
                ShippingDistrict = request.ShippingDistrict,
                ShippingCity = request.ShippingCity,
                ShippingCountry = request.ShippingCountry,
                ShippingPostalCode = request.ShippingPostalCode,
                
                VoucherCode = appliedVoucherCode,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 5. Tạo OrderItems từ CartItems
            foreach (var cartItem in cart.Items)
            {
                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    VariantId = cartItem.VariantId,
                    ProductName = cartItem.Product.Name,
                    ProductSku = cartItem.Product.Sku,
                    VariantName = cartItem.Variant?.Name,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    DiscountAmount = cartItem.DiscountAmount,
                    TotalPrice = cartItem.TotalPrice,
                    FulfilledQuantity = 0,
                    ReturnedQuantity = 0,
                    CancelledQuantity = 0
                };
                order.Items.Add(orderItem);
            }

            // 6. Lưu order vào database
            await _dbContext.Orders.AddAsync(order, cancellationToken);
            
            // 7. Reserve inventory (kiểm tra và giữ chỗ tồn kho)
            await ReserveInventoryAsync(order, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            // 8. Bắt đầu Saga orchestration
            _ = Task.Run(async () =>
            {
                try
                {
                    await _orderSaga.ExecuteAsync(order.Id, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Saga execution failed for order {OrderId}", order.Id);
                }
            }, cancellationToken);

            // 9. Xử lý thanh toán nếu không phải COD
            string? paymentUrl = null;
            bool requiresPayment = false;

            if (request.PaymentMethod == "Stripe")
            {
                var paymentResult = await _paymentService.CreatePaymentIntentAsync(order.Id, totalAmount, cancellationToken);
                paymentUrl = paymentResult.PaymentUrl;
                requiresPayment = true;
            }
            else if (request.PaymentMethod == "PayOS")
            {
                var paymentResult = await _paymentService.CreatePayOSLinkAsync(order.Id, totalAmount, cancellationToken);
                paymentUrl = paymentResult.PaymentUrl;
                requiresPayment = true;
            }
            else if (request.PaymentMethod == "Wallet" && request.UseWalletBalance)
            {
                // Trừ trực tiếp từ wallet
                await DeductFromWalletAsync(request.UserId, totalAmount, order.Id, cancellationToken);
                order.PaymentStatus = PaymentStatus.Captured;
                order.Status = OrderStatus.Paid;
                order.PaidAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Created order {OrderNumber} for user {UserId}", orderNumber, request.UserId);

            return new CreateOrderResult
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                TotalAmount = totalAmount,
                Status = order.Status.ToString(),
                PaymentUrl = paymentUrl,
                RequiresPayment = requiresPayment
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create order for user {UserId}", request.UserId);
            throw;
        }
    }

    private async Task<(string? VoucherCode, decimal DiscountAmount)> ApplyVoucherAsync(
        string voucherCode, decimal subtotal, Guid userId, CancellationToken cancellationToken)
    {
        var voucher = await _dbContext.Vouchers
            .FirstOrDefaultAsync(v => v.Code == voucherCode && v.IsActive, cancellationToken);

        if (voucher == null)
            return (null, 0);

        // Kiểm tra điều kiện áp dụng
        if (subtotal < voucher.MinOrderAmount)
            return (null, 0);

        if (voucher.UsageCount >= voucher.TotalUsageLimit)
            return (null, 0);

        if (voucher.StartDate > DateTime.UtcNow || voucher.EndDate < DateTime.UtcNow)
            return (null, 0);

        // Tính discount amount
        decimal discountAmount = voucher.Type switch
        {
            VoucherType.Percentage => Math.Min(subtotal * voucher.Value / 100, voucher.MaxDiscountAmount ?? subtotal),
            VoucherType.FixedAmount => Math.Min(voucher.Value, subtotal),
            VoucherType.FreeShipping => 0, // Sẽ xử lý ở phần shipping
            _ => 0
        };

        // Tăng usage count
        voucher.UsageCount++;

        return (voucher.Code, discountAmount);
    }

    private decimal CalculateShippingFee(string city, decimal subtotal)
    {
        // Tạm tính cố định, sẽ tích hợp API GHN/GHTK sau
        // Hà Nội và HCM: 30k, các tỉnh khác: 50k
        if (city.Contains("Hà Nội") || city.Contains("Hồ Chí Minh") || city.Contains("TPHCM"))
            return 30000;
        return 50000;
    }

    private string GenerateOrderNumber()
    {
        // Format: ORD-YYYYMMDD-XXXXX
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = new Random().Next(10000, 99999);
        return $"ORD-{datePart}-{randomPart}";
    }

    private PaymentMethod ParsePaymentMethod(string method) => method switch
    {
        "Stripe" => PaymentMethod.CreditCard,
        "PayOS" => PaymentMethod.BankTransfer,
        "Wallet" => PaymentMethod.Wallet,
        _ => PaymentMethod.COD
    };

    private async Task ReserveInventoryAsync(Order order, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items)
        {
            // Tìm lot có sẵn (FEFO - First Expired, First Out)
            var lot = await _dbContext.InventoryLots
                .Where(l => l.ProductId == item.ProductId 
                           && l.VariantId == item.VariantId
                           && l.AvailableQuantity >= item.Quantity
                           && l.Status == InventoryStatus.Available)
                .OrderBy(l => l.ExpiryDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (lot == null)
                throw new InvalidOperationException($"Không đủ tồn kho cho sản phẩm {item.ProductName}");

            // Reserve stock
            lot.AvailableQuantity -= item.Quantity;
            lot.ReservedQuantity += item.Quantity;

            // Tạo stock movement record
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                ProductId = item.ProductId,
                VariantId = item.VariantId,
                LotId = lot.Id,
                QuantityChange = -item.Quantity,
                QuantityBefore = lot.AvailableQuantity + item.Quantity,
                QuantityAfter = lot.AvailableQuantity,
                Type = StockMovementType.Reservation,
                ReferenceType = "Order",
                ReferenceId = order.Id,
                Reason = $"Reserve for order {order.OrderNumber}",
                PerformedBy = order.UserId
            };
            _dbContext.StockMovements.Add(movement);
        }
    }

    private async Task DeductFromWalletAsync(Guid userId, decimal amount, Guid orderId, CancellationToken cancellationToken)
    {
        var wallet = await _dbContext.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

        if (wallet == null || wallet.Balance < amount)
            throw new InvalidOperationException("Số dư ví không đủ");

        var balanceBefore = wallet.Balance;
        wallet.Balance -= amount;

        var transaction = new WalletTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderId = orderId,
            Type = WalletTransactionType.Debit,
            Amount = amount,
            BalanceBefore = balanceBefore,
            BalanceAfter = wallet.Balance,
            Description = $"Thanh toán đơn hàng {orderId}",
            ReferenceType = "Order",
            ReferenceId = orderId
        };
        _dbContext.WalletTransactions.Add(transaction);
    }
}
