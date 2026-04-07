namespace BeautyEcommerce.Application.Features.Auth.Commands.Register;

using MediatR;
using BeautyEcommerce.Application.Features.Auth.DTOs;

public record RegisterCommand : IRequest<Result<AuthResultDto>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}
