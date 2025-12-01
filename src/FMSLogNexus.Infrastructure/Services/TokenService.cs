using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;
using FMSLogNexus.Core.Interfaces.Services;
using TokenValidationResult = FMSLogNexus.Core.Interfaces.Services.TokenValidationResult;

namespace FMSLogNexus.Infrastructure.Services;

/// <summary>
/// Service implementation for JWT token operations.
/// </summary>
public class TokenService : ITokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;
    private readonly int _refreshTokenExpirationDays;
    private readonly int _extendedRefreshTokenExpirationDays;

    public TokenService(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        _issuer = configuration["Jwt:Issuer"] ?? "FMSLogNexus";
        _audience = configuration["Jwt:Audience"] ?? "FMSLogNexusClients";
        _accessTokenExpirationMinutes = int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");
        _refreshTokenExpirationDays = int.Parse(configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        _extendedRefreshTokenExpirationDays = int.Parse(configuration["Jwt:ExtendedRefreshTokenExpirationDays"] ?? "30");
    }

    /// <inheritdoc />
    public TimeSpan AccessTokenExpiration => TimeSpan.FromMinutes(_accessTokenExpirationMinutes);

    /// <inheritdoc />
    public TimeSpan RefreshTokenExpiration => TimeSpan.FromDays(_refreshTokenExpirationDays);

    /// <inheritdoc />
    public TimeSpan ExtendedRefreshTokenExpiration => TimeSpan.FromDays(_extendedRefreshTokenExpirationDays);

    /// <inheritdoc />
    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("security_stamp", user.SecurityStamp),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrEmpty(user.DisplayName))
        {
            claims.Add(new Claim("display_name", user.DisplayName));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateAccessToken(
        Guid userId,
        string username,
        UserRole role,
        IEnumerable<Claim>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(ClaimTypes.Role, role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (additionalClaims != null)
        {
            claims.AddRange(additionalClaims);
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public TokenValidationResult ValidateAndExtract(string token)
    {
        return ValidateAccessToken(token);
    }

    /// <inheritdoc />
    public TokenValidationResult ValidateAccessToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return TokenValidationResult.Invalid(ErrorCodes.TokenInvalid, "Invalid token algorithm.");
            }

            var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                         principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var username = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value ??
                           principal.FindFirst(ClaimTypes.Name)?.Value;

            var roleString = principal.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(roleString))
            {
                return TokenValidationResult.Invalid(ErrorCodes.TokenInvalid, "Token missing required claims.");
            }

            if (!Guid.TryParse(userId, out var userGuid))
            {
                return TokenValidationResult.Invalid(ErrorCodes.TokenInvalid, "Invalid user ID in token.");
            }

            if (!Enum.TryParse<UserRole>(roleString, out var role))
            {
                return TokenValidationResult.Invalid(ErrorCodes.TokenInvalid, "Invalid role in token.");
            }

            return TokenValidationResult.Valid(userGuid, username, role);
        }
        catch (SecurityTokenExpiredException)
        {
            return TokenValidationResult.Invalid(ErrorCodes.TokenExpired, "Token has expired.");
        }
        catch (SecurityTokenException ex)
        {
            return TokenValidationResult.Invalid(ErrorCodes.TokenInvalid, ex.Message);
        }
        catch (Exception)
        {
            return TokenValidationResult.Invalid(ErrorCodes.TokenInvalid, "Token validation failed.");
        }
    }

    /// <inheritdoc />
    public int GetAccessTokenExpirationMinutes() => _accessTokenExpirationMinutes;

    /// <inheritdoc />
    public int GetRefreshTokenExpirationDays() => _refreshTokenExpirationDays;

    /// <inheritdoc />
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = false // Allow expired tokens
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public Guid? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal == null)
            return null;

        var userId = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ??
                     principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(userId, out var guid) ? guid : null;
    }

    /// <inheritdoc />
    public string? GetSecurityStampFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst("security_stamp")?.Value;
    }

    /// <inheritdoc />
    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc />
    public string HashApiKey(string apiKey)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc />
    public string GenerateApiKey()
    {
        var prefix = "fms_" + GenerateRandomString(8);
        var secret = GenerateRandomString(32);
        return $"{prefix}.{secret}";
    }

    /// <inheritdoc />
    public string GetApiKeyPrefix(string apiKey)
    {
        return apiKey.Length >= 8 ? apiKey[..8] : apiKey;
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}
