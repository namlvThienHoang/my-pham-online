namespace BeautyEcommerce.Application.Features.Auth.Commands.VerifyMfa;

using MediatR;
using OtpNet;

public class VerifyMfaCommandHandler : IRequestHandler<VerifyMfaCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public VerifyMfaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(VerifyMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<bool>.Failure("User not found");
        }

        if (string.IsNullOrEmpty(user.MfaSecret))
        {
            return Result<bool>.Failure("MFA secret not found. Please enable MFA first.");
        }

        var totp = new Totp(Base32Encoding.ToBytes(user.MfaSecret));
        var isValid = totp.VerifyTotp(request.Code, out _, new VerificationWindow(2, 0));

        if (!isValid)
        {
            return Result<bool>.Failure("Invalid MFA code");
        }

        // MFA is already enabled when secret was generated, but we could add additional confirmation here
        return Result<bool>.Success(true);
    }
}
