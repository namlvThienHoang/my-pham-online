namespace BeautyEcommerce.Application.Features.Auth.Commands.Logout;

using MediatR;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result<Unit>>
{
    private readonly IUnitOfWork _unitOfWork;

    public LogoutCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Unit>> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (request.LogoutAllDevices)
        {
            // Revoke all refresh tokens for this user
            var allTokens = await _unitOfWork.RefreshTokens.GetByUserIdAsync(request.UserId, cancellationToken);
            foreach (var token in allTokens.Where(t => t.IsActive))
            {
                token.Revoke();
            }
        }
        else if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            // Revoke specific refresh token
            var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);
            if (refreshToken != null && refreshToken.UserId == request.UserId)
            {
                refreshToken.Revoke();
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<Unit>.Success(Unit.Value);
    }
}
