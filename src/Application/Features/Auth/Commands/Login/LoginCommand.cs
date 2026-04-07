namespace BeautyEcommerce.Application.Features.Auth.Commands.Login;

using MediatR;
using BeautyEcommerce.Application.Features.Auth.DTOs;

public record LoginCommand : IRequest<Result<AuthResultDto>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? MfaCode { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
