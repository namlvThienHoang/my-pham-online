namespace BeautyEcommerce.Application.Commands.Cart;

using MediatR;
using FluentValidation;

/// <summary>
/// Command thêm item vào giỏ hàng (với atomic inventory reservation)
/// </summary>
public record AddToCartCommand : IRequest<CartResult>
{
    public Guid UserId { get; init; }
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public int Quantity { get; init; } = 1;
}

public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");
        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product ID is required");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(99).WithMessage("Quantity cannot exceed 99 items");
    }
}

/// <summary>
/// Command cập nhật số lượng item trong giỏ
/// </summary>
public record UpdateCartItemCommand : IRequest<CartResult>
{
    public Guid UserId { get; init; }
    public Guid CartItemId { get; init; }
    public int Quantity { get; init; }
}

/// <summary>
/// Command xóa item khỏi giỏ
/// </summary>
public record RemoveFromCartCommand : IRequest<Unit>
{
    public Guid UserId { get; init; }
    public Guid CartItemId { get; init; }
}

/// <summary>
/// Command áp dụng voucher vào giỏ
/// </summary>
public record ApplyVoucherCommand : IRequest<CartResult>
{
    public Guid UserId { get; init; }
    public string VoucherCode { get; init; } = string.Empty;
}

/// <summary>
/// Command checkout giỏ hàng (trigger Order Saga)
/// </summary>
public record CheckoutCommand : IRequest<CheckoutResult>
{
    public Guid UserId { get; init; }
    public Guid? ShippingAddressId { get; init; }
    public string PaymentMethod { get; init; } = "COD";
    public string? VoucherCode { get; init; }
    public string? CustomerNote { get; init; }
    public string? IdempotencyKey { get; init; }
}

public class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required");
        RuleFor(x => x.PaymentMethod).NotEmpty().WithMessage("Payment method is required")
            .Must(pm => new[] { "COD", "CreditCard", "BankTransfer", "Wallet" }.Contains(pm))
            .WithMessage("Invalid payment method");
    }
}

public class CartResult
{
    public bool Success { get; init; }
    public CartDto? Cart { get; init; }
    public string? Error { get; init; }
    public string[]? ValidationErrors { get; init; }
}

public class CheckoutResult
{
    public bool Success { get; init; }
    public Guid? OrderId { get; init; }
    public string? OrderNumber { get; init; }
    public string? Error { get; init; }
    public string? NextAction { get; init; }
    public string? PaymentUrl { get; init; }
}

public class CartDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public List<CartItemDto> Items { get; init; } = new();
    public decimal Subtotal { get; init; }
    public decimal Discount { get; init; }
    public decimal Tax { get; init; }
    public decimal ShippingFee { get; init; }
    public decimal Total { get; init; }
    public string CurrencyCode { get; init; } = "VND";
    public List<AppliedVoucherDto> AppliedVouchers { get; init; } = new();
    public DateTime? ExpiresAt { get; init; }
}

public class CartItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public Guid? VariantId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductSlug { get; init; } = string.Empty;
    public string? Image { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public int InventoryAvailable { get; init; }
    public List<ProductOptionDto> Options { get; init; } = new();
}

public class ProductOptionDto
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

public class AppliedVoucherDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DiscountType { get; init; } = string.Empty;
    public decimal DiscountValue { get; init; }
    public decimal? MaxDiscountAmount { get; init; }
    public decimal DiscountAmount { get; init; }
}
