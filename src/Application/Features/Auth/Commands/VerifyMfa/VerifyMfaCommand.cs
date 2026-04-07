namespace BeautyEcommerce.Application.Features.Auth.Commands.VerifyMfa;

using MediatR;

public record VerifyMfaCommand : IRequest<Result<bool>>
{
    public Guid UserId { get; init; }
    public string Code { get; init; } = string.Empty;
}
