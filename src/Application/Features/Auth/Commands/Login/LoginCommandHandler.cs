namespace BeautyEcommerce.Application.Features.Auth.Commands.Login;

using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BeautyEcommerce.Application.Features.Auth.DTOs;
using BeautyEcommerce.Domain.Entities;
using BeautyEcommerce.Domain.Enums;
using OtpNet;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSettings _jwtSettings;

    public LoginCommandHandler(IUnitOfWork unitOfWork, IOptions<JwtSettings> jwtSettings)
    {
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Get user by email
        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            return Result<AuthResultDto>.Failure("Invalid email or password");
        }

        // Check if user is locked
        if (user.IsLocked())
        {
            return Result<AuthResultDto>.Failure($"Account is locked until {user.LockedUntil.Value:HH:mm:ss}");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return Result<AuthResultDto>.Failure("Account is deactivated");
        }

        // Verify password
        var passwordHasher = new PasswordHasher<User>();
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            user.RecordFailedLogin();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            if (user.IsLocked())
            {
                return Result<AuthResultDto>.Failure("Too many failed attempts. Account locked for 15 minutes.");
            }
            
            return Result<AuthResultDto>.Failure("Invalid email or password");
        }

        // Check MFA
        if (user.MfaEnabled)
        {
            if (string.IsNullOrEmpty(request.MfaCode))
            {
                return Result<AuthResultDto>.Failure("MFA code required", "MFA_REQUIRED");
            }

            if (user.MfaSecret == null)
            {
                return Result<AuthResultDto>.Failure("MFA is enabled but no secret found");
            }

            var totp = new Totp(Base32Encoding.ToBytes(user.MfaSecret));
            var isValid = totp.VerifyTotp(request.MfaCode, out _, new VerificationWindow(2, 0));
            
            if (!isValid)
            {
                user.RecordFailedLogin();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result<AuthResultDto>.Failure("Invalid MFA code");
            }
        }

        // Unlock user if previously locked
        if (user.FailedLoginAttempts > 0)
        {
            user.Unlock();
        }

        // Update last login
        user.UpdateLastLogin();

        // Generate tokens
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken(user.Id, request.IpAddress, request.UserAgent);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

        var authResult = new AuthResultDto(
            accessToken,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            userDto
        );

        return Result<AuthResultDto>.Success(authResult);
    }

    private string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName ?? string.Empty),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress, string? userAgent)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        var refreshToken = RefreshToken.Create(userId, token, expiresAt, null, ipAddress, userAgent);
        _unitOfWork.RefreshTokens.Add(refreshToken);

        return refreshToken;
    }
}
