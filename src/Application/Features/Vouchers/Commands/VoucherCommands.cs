namespace BeautyEcommerce.Application.Features.Vouchers.Commands;

using MediatR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using BeautyEcommerce.Infrastructure.Persistence;
using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Enums;

/// <summary>
/// Command tạo voucher mới (admin only)
/// </summary>
public class CreateVoucherCommand : IRequest<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = "FixedAmount"; // Percentage, FixedAmount, FreeShipping
    public decimal Value { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal MinOrderAmount { get; set; }
    public int TotalUsageLimit { get; set; } = 1000;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool IsStackable { get; set; } = false;
    public List<string>? ApplicableProducts { get; set; }
    public List<string>? ApplicableCategories { get; set; }
}

public class CreateVoucherCommandValidator : AbstractValidator<CreateVoucherCommand>
{
    public CreateVoucherCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã voucher là bắt buộc")
            .MaximumLength(50);
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên voucher là bắt buộc")
            .MaximumLength(200);
        
        RuleFor(x => x.Value)
            .GreaterThan(0).WithMessage("Giá trị voucher phải lớn hơn 0");
        
        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate).WithMessage("Ngày bắt đầu phải trước ngày kết thúc");
        
        RuleFor(x => x.Type)
            .Must(t => new[] { "Percentage", "FixedAmount", "FreeShipping" }.Contains(t))
            .WithMessage("Loại voucher không hợp lệ");
        
        RuleFor(x => x.MinOrderAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Giá trị đơn hàng tối thiểu phải >= 0");
    }
}

/// <summary>
/// Handler xử lý tạo voucher
/// </summary>
public class CreateVoucherCommandHandler : IRequestHandler<CreateVoucherCommand, Guid>
{
    private readonly AppDbContext _dbContext;

    public CreateVoucherCommandHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreateVoucherCommand request, CancellationToken cancellationToken)
    {
        // Kiểm tra code trùng
        var existingVoucher = await _dbContext.Vouchers
            .FirstOrDefaultAsync(v => v.Code == request.Code, cancellationToken);

        if (existingVoucher != null)
            throw new InvalidOperationException($"Mã voucher '{request.Code}' đã tồn tại");

        var voucher = new Voucher
        {
            Id = Guid.NewGuid(),
            Code = request.Code.ToUpper(),
            Name = request.Name,
            Description = request.Description,
            Type = ParseVoucherType(request.Type),
            Value = request.Value,
            MaxDiscountAmount = request.MaxDiscountAmount,
            MinOrderAmount = request.MinOrderAmount,
            TotalUsageLimit = request.TotalUsageLimit,
            UsageCount = 0,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true,
            IsPublic = request.IsPublic,
            IsStackable = request.IsStackable,
            ApplicableProducts = request.ApplicableProducts != null 
                ? System.Text.Json.JsonSerializer.Serialize(request.ApplicableProducts) 
                : null,
            ApplicableCategories = request.ApplicableCategories != null 
                ? System.Text.Json.JsonSerializer.Serialize(request.ApplicableCategories) 
                : null,
            CreatedAt = DateTime.UtcNow
        };

        await _dbContext.Vouchers.AddAsync(voucher, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return voucher.Id;
    }

    private static VoucherType ParseVoucherType(string type) => type switch
    {
        "Percentage" => VoucherType.Percentage,
        "FixedAmount" => VoucherType.FixedAmount,
        "FreeShipping" => VoucherType.FreeShipping,
        _ => VoucherType.FixedAmount
    };
}

/// <summary>
/// Command áp dụng voucher vào order
/// </summary>
public class ApplyVoucherToOrderCommand : IRequest<ApplyVoucherResult>
{
    public Guid OrderId { get; set; }
    public string VoucherCode { get; set; } = string.Empty;
}

public class ApplyVoucherResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal NewTotal { get; set; }
}

public class ApplyVoucherToOrderCommandHandler : IRequestHandler<ApplyVoucherToOrderCommand, ApplyVoucherResult>
{
    private readonly AppDbContext _dbContext;

    public ApplyVoucherToOrderCommandHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApplyVoucherResult> Handle(ApplyVoucherToOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _dbContext.Orders.FindAsync(new object[] { request.OrderId }, cancellationToken);
        if (order == null)
            return new ApplyVoucherResult { Success = false, Message = "Đơn hàng không tồn tại" };

        // Không cho phép áp dụng voucher nếu order đã thanh toán
        if (order.PaymentStatus == PaymentStatus.Captured || order.PaymentStatus == PaymentStatus.Refunded)
            return new ApplyVoucherResult { Success = false, Message = "Không thể áp dụng voucher cho đơn hàng đã thanh toán" };

        var voucher = await _dbContext.Vouchers
            .FirstOrDefaultAsync(v => v.Code == request.VoucherCode && v.IsActive, cancellationToken);

        if (voucher == null)
            return new ApplyVoucherResult { Success = false, Message = "Mã voucher không hợp lệ" };

        // Kiểm tra điều kiện
        if (!voucher.StartDate.Date.Equals(DateTime.UtcNow.Date) && voucher.StartDate > DateTime.UtcNow)
            return new ApplyVoucherResult { Success = false, Message = "Voucher chưa đến thời gian sử dụng" };

        if (voucher.EndDate < DateTime.UtcNow)
            return new ApplyVoucherResult { Success = false, Message = "Voucher đã hết hạn" };

        if (voucher.UsageCount >= voucher.TotalUsageLimit)
            return new ApplyVoucherResult { Success = false, Message = "Voucher đã hết lượt sử dụng" };

        if (order.Subtotal < voucher.MinOrderAmount)
            return new ApplyVoucherResult 
            { 
                Success = false, 
                Message = $"Đơn hàng cần đạt tối thiểu {voucher.MinOrderAmount:N0}đ để áp dụng voucher này" 
            };

        // Tính discount amount
        decimal discountAmount = voucher.Type switch
        {
            VoucherType.Percentage => Math.Min(order.Subtotal * voucher.Value / 100, voucher.MaxDiscountAmount ?? order.Subtotal),
            VoucherType.FixedAmount => Math.Min(voucher.Value, order.Subtotal),
            VoucherType.FreeShipping => order.ShippingAmount,
            _ => 0
        };

        // Cập nhật order
        var oldDiscount = order.DiscountAmount;
        order.DiscountAmount = discountAmount;
        order.Total = order.Subtotal - discountAmount + order.ShippingAmount;
        order.VoucherCode = voucher.Code;

        // Tăng usage count
        voucher.UsageCount++;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ApplyVoucherResult
        {
            Success = true,
            Message = "Áp dụng voucher thành công",
            DiscountAmount = discountAmount,
            NewTotal = order.Total
        };
    }
}
