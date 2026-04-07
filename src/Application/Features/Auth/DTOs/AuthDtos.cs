namespace BeautyEcommerce.Application.Features.Auth.DTOs;

public record AuthResultDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record UserDto(
    Guid Id,
    string Email,
    string? FullName,
    string? PhoneNumber,
    string Role,
    bool IsEmailVerified,
    bool IsMfaEnabled,
    string? AvatarUrl
);

public record RegisterRequestDto(
    string Email,
    string Password,
    string? FullName,
    string? PhoneNumber
);

public record LoginRequestDto(
    string Email,
    string Password,
    string? MfaCode,
    string? IpAddress,
    string? UserAgent
);

public record RefreshTokenRequestDto(
    string RefreshToken,
    string? IpAddress,
    string? UserAgent
);

public record EnableMfaResponseDto(
    string Secret,
    string QrCodeUrl,
    string ManualEntryKey
);

public record VerifyMfaRequestDto(
    string Code
);

public record LogoutRequestDto(
    string? RefreshToken
);
