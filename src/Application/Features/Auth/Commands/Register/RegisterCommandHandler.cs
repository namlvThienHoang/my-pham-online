namespace BeautyEcommerce.Application.Features.Auth.Commands.Register;

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

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSettings _jwtSettings;

    public RegisterCommandHandler(IUnitOfWork unitOfWork, IOptions<JwtSettings> jwtSettings)
    {
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<AuthResultDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if user already exists
        var existingUser = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser != null)
        {
            return Result<AuthResultDto>.Failure("Email already registered");
        }

        // Hash password
        var passwordHasher = new PasswordHasher<User>();
        var passwordHash = passwordHasher.HashPassword(null, request.Password);

        // Create user
        var user = User.Create(request.Email, passwordHash, request.FullName);
        if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            user.UpdateProfile(null, request.PhoneNumber, null);
        }

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
