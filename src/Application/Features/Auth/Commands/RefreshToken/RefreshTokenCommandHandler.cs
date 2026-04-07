namespace BeautyEcommerce.Application.Features.Auth.Commands.RefreshToken;

using MediatR;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BeautyEcommerce.Application.Features.Auth.DTOs;
using BeautyEcommerce.Domain.Entities;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenCommandHandler(IUnitOfWork unitOfWork, IOptions<JwtSettings> jwtSettings)
    {
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Find refresh token
        var refreshToken = await _unitOfWork.RefreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);
        
        if (refreshToken == null || !refreshToken.IsActive)
        {
            return Result<AuthResultDto>.Failure("Invalid or expired refresh token");
        }

        // Get user
        var user = await _unitOfWork.Users.GetByIdAsync(refreshToken.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            return Result<AuthResultDto>.Failure("User not found or inactive");
        }

        // Check for token family compromise (reuse attack detection)
        if (!string.IsNullOrEmpty(refreshToken.FamilyId))
        {
            var existingTokensInFamily = await _unitOfWork.RefreshTokens.GetByFamilyIdAsync(refreshToken.FamilyId, cancellationToken);
            var activeTokensInFamily = existingTokensInFamily.Where(t => t.IsActive).ToList();
            
            // If there are other active tokens in the same family, this could be a reuse attack
            // Revoke all tokens in the family for security
            if (activeTokensInFamily.Count > 1)
            {
                foreach (var token in activeTokensInFamily)
                {
                    token.Revoke();
                }
                return Result<AuthResultDto>.Failure("Security alert: Token reuse detected. All tokens revoked.");
            }
        }

        // Revoke old token
        var newRefreshToken = GenerateRefreshToken(user.Id, request.IpAddress, request.UserAgent, refreshToken.FamilyId);
        refreshToken.Revoke(newRefreshToken.Token);

        // Generate new access token
        var accessToken = GenerateAccessToken(user);

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
            newRefreshToken.Token,
            newRefreshToken.ExpiresAt,
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

    private RefreshToken GenerateRefreshToken(Guid userId, string? ipAddress, string? userAgent, string? familyId = null)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);

        var refreshToken = RefreshToken.Create(userId, token, expiresAt, familyId, ipAddress, userAgent);
        _unitOfWork.RefreshTokens.Add(refreshToken);

        return refreshToken;
    }
}
