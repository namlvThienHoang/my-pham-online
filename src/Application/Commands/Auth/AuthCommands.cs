namespace BeautyEcommerce.Application.Commands.Auth;

using MediatR;
using FluentValidation;

/// <summary>
/// Command đăng ký user mới
/// </summary>
public record RegisterUserCommand : IRequest<AuthResult>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
}

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)").WithMessage("Password must contain uppercase, lowercase and number");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+?[\d\s-]{10,}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
            .WithMessage("Invalid phone number format");
    }
}

/// <summary>
/// Command đăng nhập
/// </summary>
public record LoginUserCommand : IRequest<AuthResult>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string? MfaCode { get; init; }
    public bool RememberMe { get; init; }
}

/// <summary>
/// Command refresh token
/// </summary>
public record RefreshTokenCommand : IRequest<AuthResult>
{
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// Command enable MFA
/// </summary>
public record EnableMfaCommand : IRequest<MfaSetupResult>
{
    public Guid UserId { get; init; }
}

/// <summary>
/// Command verify MFA
/// </summary>
public record VerifyMfaCommand : IRequest<bool>
{
    public Guid UserId { get; init; }
    public string Code { get; init; } = string.Empty;
}

/// <summary>
/// Result cho authentication operations
/// </summary>
public class AuthResult
{
    public bool Success { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; }
    public UserDto? User { get; init; }
    public string? Error { get; init; }
    public bool RequiresMfa { get; init; }
    public string? MfaTempToken { get; init; }
}

public class UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string Role { get; init; } = string.Empty;
    public bool IsEmailVerified { get; init; }
    public bool MfaEnabled { get; init; }
    public string? AvatarUrl { get; init; }
}

public class MfaSetupResult
{
    public bool Success { get; init; }
    public string? Secret { get; init; }
    public string? QrCodeUrl { get; init; }
    public string[]? RecoveryCodes { get; init; }
    public string? Error { get; init; }
}
