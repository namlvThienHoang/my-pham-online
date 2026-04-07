namespace BeautyEcommerce.Application.Features.Auth.Commands.Logout;

using MediatR;

public record LogoutCommand : IRequest<Result<Unit>>
{
    public Guid UserId { get; init; }
    public string? RefreshToken { get; init; }
    public bool LogoutAllDevices { get; init; }
}
