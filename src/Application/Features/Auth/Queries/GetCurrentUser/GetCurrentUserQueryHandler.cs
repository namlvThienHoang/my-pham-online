namespace BeautyEcommerce.Application.Features.Auth.Queries.GetCurrentUser;

using MediatR;
using BeautyEcommerce.Application.Features.Auth.DTOs;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetCurrentUserQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null)
        {
            return Result<UserDto>.Failure("User not found");
        }

        var userDto = new UserDto(
            user.Id,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.Role.ToString(),
            user.IsEmailVerified,
            user.MfaEnabled,
            user.AvatarUrl
        );

        return Result<UserDto>.Success(userDto);
    }
}
