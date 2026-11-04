using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SimpleWebAppReact.Entities;

namespace SimpleWebAppReact.Services;

/// <summary>
/// simple JWT token generation
/// </summary>
public class JwtService
{
    private const string Issuer = "boilercrib";
    private const string Audience = "boilercrib-app";
    private const string RoleClaim = "role";
    private const int MinimumKeyBytes = 64;
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(7);

    private readonly JsonWebTokenHandler _tokenHandler = new();
    private readonly SigningCredentials _signingCredentials;

    public JwtService(IConfiguration configuration)
    {
        var key = configuration["JWT_KEY"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"JWT_KEY not found in .env, ensure it is set.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        _signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        ValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = signingKey
        };
    }

    public TokenValidationParameters ValidationParameters { get; }

    /// <summary>
    /// Generate a JWT token for a given user for authentication.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public string CreateToken(User user)
    {
        ArgumentException.ThrowIfNullOrEmpty(user.Id);

        return _tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.Add(TokenLifetime),
            SigningCredentials = _signingCredentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id,
                [JwtRegisteredClaimNames.UniqueName] = user.Username ?? string.Empty,
                [RoleClaim] = RoleOf(user).ToString()
            }
        });
    }

    private static UserType RoleOf(User user) =>
        user.AccountType is int accountType && Enum.IsDefined(typeof(UserType), accountType)
            ? (UserType)accountType
            : UserType.Student;
}
