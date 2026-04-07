namespace BeautyEcommerce.Application.Features.Auth.Commands.EnableMfa;

using MediatR;
using BeautyEcommerce.Application.Features.Auth.DTOs;
using OtpNet;

public class EnableMfaCommandHandler : IRequestHandler<EnableMfaCommand, Result<EnableMfaResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public EnableMfaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EnableMfaResponseDto>> Handle(EnableMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<EnableMfaResponseDto>.Failure("User not found");
        }

        // Generate new secret key
        var secretKey = KeyGeneration.GenerateRandomKey(20);
        var base32Secret = Base32Encoding.ToString(secretKey);

        // Save secret to user (not enabled yet, will be enabled after verification)
        user.EnableMfa(base32Secret);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate QR code URL
        var issuer = "BeautyEcommerce";
        var label = $"{issuer}:{user.Email}";
        var qrCodeUrl = $"https://chart.googleapis.com/chart?chs=200x200&cht=qr&chl={Uri.EscapeDataString($"otpauth://totp/{label}?secret={base32Secret}&issuer={issuer}")}";

        return Result<EnableMfaResponseDto>.Success(new EnableMfaResponseDto(
            base32Secret,
            qrCodeUrl,
            base32Secret
        ));
    }
}
