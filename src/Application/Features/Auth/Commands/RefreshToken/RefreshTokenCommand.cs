namespace BeautyEcommerce.Application.Features.Auth.Commands.RefreshToken;

using MediatR;
using BeautyEcommerce.Application.Features.Auth.DTOs;

public record RefreshTokenCommand : IRequest<Result<AuthResultDto>>
{
    public string RefreshToken { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
