using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ListenTogether.Api.DbContext;
using ListenTogether.Api.Entities;
using ListenTogether.Api.Models;

namespace ListenTogether.Api.Authorization;
public interface IJwtUtils
{
    public string GenerateToken(UserEntity user, string deviceId);
    public int? ValidateToken(string? token);
    public RefreshTokenEntity GenerateRefreshToken(string deviceId);
}

public class JwtUtils : IJwtUtils
{
    private readonly DataContext _context;
    private readonly AppSettings _appSettings;

    public JwtUtils(DataContext context, IOptions<AppSettings> appSettings)
    {
        _context = context;
        _appSettings = appSettings.Value;
    }

    public string GenerateToken(UserEntity user, string deviceId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_appSettings.Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                new[]
                {
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("DeviceId",deviceId)
                }),
            Expires = DateTime.Now.AddMinutes(2 * 60),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public int? ValidateToken(string? token)
    {
        if (token == null)
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_appSettings.Secret);
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var securityToken = (JwtSecurityToken)validatedToken;
            var id = securityToken.Claims.First(x => x.Type == "UserId").Value;

            // return user id from JWT token if validation successful
            return Convert.ToInt32(id);
        }
        catch
        {
            // return null if validation fails
            return null;
        }
    }

    public RefreshTokenEntity GenerateRefreshToken(string deviceId)
    {
        var refreshToken = new RefreshTokenEntity
        {
            DeviceId = deviceId,
            Token = GetUniqueToken(),
            ExpireTime = DateTime.Now.AddDays(_appSettings.TokenExpireDay),
            CreateTime = DateTime.Now
        };

        return refreshToken;
    }
    private string GetUniqueToken()
    {
        // token is a cryptographically strong random sequence of values
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        // ensure token is unique by checking against db
        var tokenExist = _context.Users.Any(u => u.RefreshTokens.Any(t => t.Token == token));
        if (tokenExist)
        {
            return GetUniqueToken();
        }

        return token;
    }
}