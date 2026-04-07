namespace BeautyEcommerce.Application.Features.Auth.Commands.EnableMfa;

using MediatR;
using BeautyEcommerce.Application.Features.Auth.DTOs;

public record EnableMfaCommand : IRequest<Result<EnableMfaResponseDto>>
{
    public Guid UserId { get; init; }
}
