using Microsoft.AspNetCore.Identity;

namespace SimpleWebAppReact.Services;

/// <summary>
/// Implements password hashing through <see cref="HashPassword"/>,
/// and password verification through <see cref="VerifyPassword"/>
/// </summary>
public class PasswordHasherService
{
    private static readonly object IgnoredUser = new();

    private readonly PasswordHasher<object> _passwordHasher = new();

    /// <summary>
    /// Hashes a password.
    /// </summary>
    public string HashPassword(string password) => _passwordHasher.HashPassword(IgnoredUser, password);

    /// <summary>
    /// Determines whether the rawtext password matches the hashed password.
    /// </summary>
    public bool VerifyPassword(string password, string? hash)
    {
        if (string.IsNullOrEmpty(hash))
        {
            return false;
        }

        try
        {
            return _passwordHasher.VerifyHashedPassword(IgnoredUser, hash, password)
                is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
