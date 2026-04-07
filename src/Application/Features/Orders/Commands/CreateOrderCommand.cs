namespace BeautyEcommerce.Application.Features.Orders.Commands;

using MediatR;
using FluentValidation;

/// <summary>
/// Command tạo đơn hàng mới từ giỏ hàng
/// </summary>
public class CreateOrderCommand : IRequest<CreateOrderResult>
{
    public Guid UserId { get; set; }
    public string? CustomerNote { get; set; }
    
    // Shipping address
    public string ShippingAddressLine1 { get; set; } = string.Empty;
    public string? ShippingAddressLine2 { get; set; }
    public string ShippingWard { get; set; } = string.Empty;
    public string ShippingDistrict { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = "VN";
    public string? ShippingPostalCode { get; set; }
    
    // Payment method
    public string PaymentMethod { get; set; } = "COD"; // COD, Stripe, PayOS, Wallet
    
    // Voucher code (optional)
    public string? VoucherCode { get; set; }
    
    // Flag để sử dụng wallet balance
    public bool UseWalletBalance { get; set; }
}

public class CreateOrderResult
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentUrl { get; set; }
    public bool RequiresPayment { get; set; }
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId là bắt buộc");
        
        RuleFor(x => x.ShippingAddressLine1)
            .NotEmpty().WithMessage("Địa chỉ giao hàng là bắt buộc")
            .MaximumLength(500);
        
        RuleFor(x => x.ShippingWard)
            .NotEmpty().WithMessage("Phường/Xã là bắt buộc");
        
        RuleFor(x => x.ShippingDistrict)
            .NotEmpty().WithMessage("Quận/Huyện là bắt buộc");
        
        RuleFor(x => x.ShippingCity)
            .NotEmpty().WithMessage("Tỉnh/Thành phố là bắt buộc");
        
        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Phương thức thanh toán là bắt buộc")
            .Must(pm => new[] { "COD", "Stripe", "PayOS", "Wallet" }.Contains(pm))
            .WithMessage("Phương thức thanh toán không hợp lệ");
    }
}
