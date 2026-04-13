namespace BeautyEcommerce.Application.Features.Cart.Handlers;

using MediatR;
using BeautyEcommerce.Application.Commands.Cart;
using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Handler for CheckoutCommand - implements checkout flow with inventory reservation
/// </summary>
public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, CheckoutResult>
{
    private readonly IRepository<Cart> _cartRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CheckoutCommandHandler> _logger;

    public CheckoutCommandHandler(
        IRepository<Cart> cartRepository,
        IOrderRepository orderRepository,
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork,
        ILogger<CheckoutCommandHandler> logger)
    {
        _cartRepository = cartRepository;
        _orderRepository = orderRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CheckoutResult> Handle(CheckoutCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // Validate idempotency key if provided
            if (!string.IsNullOrEmpty(request.IdempotencyKey))
            {
                // Check if this idempotency key was already processed
                // This would typically be stored in a separate table
                // For now, we'll skip duplicate check implementation
            }

            // Get user's cart with items loaded
            var carts = await _cartRepository.GetAllAsync(cancellationToken);
            var cart = carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .Include(c => c.Vouchers)
                    .ThenInclude(v => v.Voucher)
                .FirstOrDefault(c => c.UserId == request.UserId && c.ExpiresAt > DateTime.UtcNow);

            if (cart == null || !cart.Items.Any())
            {
                return new CheckoutResult
                {
                    Success = false,
                    Error = "Cart is empty or not found"
                };
            }

            // Validate all items have sufficient stock
            foreach (var item in cart.Items)
            {
                var availableStock = await _inventoryRepository.GetAvailableStockAsync(
                    item.ProductId,
                    item.VariantId,
                    cancellationToken);

                if (availableStock < item.Quantity)
                {
                    return new CheckoutResult
                    {
                        Success = false,
                        Error = $"Insufficient stock for product {item.Product.Name}. Available: {availableStock}, Requested: {item.Quantity}"
                    };
                }
            }

            // Validate voucher if provided
            if (!string.IsNullOrEmpty(request.VoucherCode))
            {
                // Voucher validation logic would go here
                // For now, we assume voucher is valid
            }

            // Create order from cart
            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                OrderNumber = GenerateOrderNumber(),
                Status = OrderStatus.Pending,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = PaymentStatus.Pending,
                ShippingAddressId = request.ShippingAddressId,
                CustomerNote = request.CustomerNote,
                Subtotal = cart.Subtotal,
                DiscountAmount = cart.DiscountAmount,
                TaxAmount = cart.TaxAmount,
                ShippingAmount = cart.ShippingAmount,
                Total = cart.Total,
                CurrencyCode = cart.CurrencyCode ?? "VND",
                CreatedAt = DateTime.UtcNow
            };

            // Add order items
            foreach (var cartItem in cart.Items)
            {
                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    VariantId = cartItem.VariantId,
                    Quantity = cartItem.Quantity,
                    UnitPrice = cartItem.UnitPrice,
                    DiscountAmount = cartItem.DiscountAmount,
                    TotalPrice = cartItem.TotalPrice,
                    CustomData = cartItem.CustomData
                };
                order.Items.Add(orderItem);
            }

            // Apply voucher to order if exists
            if (cart.Vouchers.Any())
            {
                foreach (var cartVoucher in cart.Vouchers)
                {
                    order.Vouchers.Add(new OrderVoucher
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        VoucherId = cartVoucher.VoucherId,
                        DiscountAmount = cartVoucher.DiscountAmount
                    });
                }
            }

            // Reserve inventory for all items
            foreach (var item in cart.Items)
            {
                var lots = await _inventoryRepository.GetAvailableLotsAsync(
                    item.ProductId,
                    item.VariantId,
                    item.Quantity,
                    cancellationToken);

                int remainingQuantity = item.Quantity;
                foreach (var lot in lots)
                {
                    if (remainingQuantity <= 0) break;

                    int quantityToReserve = Math.Min(remainingQuantity, lot.AvailableQuantity);
                    await _inventoryRepository.ReserveInventoryAsync(
                        lot.Id,
                        quantityToReserve,
                        order.Id,
                        cancellationToken);

                    remainingQuantity -= quantityToReserve;
                }
            }

            // Save order
            await _orderRepository.CreateAsync(order, cancellationToken);

            // Clear cart after successful checkout
            cart.Items.Clear();
            cart.Vouchers.Clear();
            cart.Subtotal = 0;
            cart.DiscountAmount = 0;
            cart.TaxAmount = 0;
            cart.ShippingAmount = 0;
            cart.Total = 0;
            await _cartRepository.UpdateAsync(cart, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Checkout successful for user {UserId}. Order {OrderNumber} created",
                request.UserId, order.OrderNumber);

            return new CheckoutResult
            {
                Success = true,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                NextAction = request.PaymentMethod switch
                {
                    "CreditCard" => "RedirectToPayment",
                    "BankTransfer" => "ShowBankDetails",
                    "Wallet" => "Complete",
                    _ => "Complete"
                },
                PaymentUrl = request.PaymentMethod == "CreditCard" ? $"/payment/{order.Id}" : null
            };
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Checkout failed for user {UserId}", request.UserId);
            return new CheckoutResult
            {
                Success = false,
                Error = "Checkout failed. Please try again."
            };
        }
    }

    private string GenerateOrderNumber()
    {
        // Format: ORD-YYYYMMDD-XXXXX
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = new Random().Next(10000, 99999);
        return $"ORD-{datePart}-{randomPart}";
    }
}
