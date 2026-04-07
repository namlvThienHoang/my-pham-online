namespace BeautyEcommerce.Application.Features.Auth.Queries.GetCurrentUser;

using MediatR;
using BeautyEcommerce.Application.Features.Auth.DTOs;

public record GetCurrentUserQuery : IRequest<Result<UserDto>>
{
    public Guid UserId { get; init; }
}
