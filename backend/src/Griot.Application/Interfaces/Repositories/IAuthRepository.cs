using Griot.Domain.Entities;

namespace Griot.Application.Interfaces.Repositories;

/// <summary>
/// Auth persistence surface. Implemented in Griot.Infrastructure (EF Core).
/// Keeps Griot.Application free of EF/HTTP/Redis — token + hashing policy lives in
/// <c>AuthService</c>, pure persistence lives behind this interface.
/// </summary>
public interface IAuthRepository
{
    /// <summary>True when any user row has the given (normalised) email.</summary>
    Task<bool> UserExistsByEmailAsync(string email);

    /// <summary>Look up a user by (normalised) email, or null when absent.</summary>
    Task<User?> FindUserByEmailAsync(string email);

    /// <summary>Persist a new user row and return it.</summary>
    Task<User> CreateUserAsync(User user);

    /// <summary>Persist a new (hashed) refresh token row and return it.</summary>
    Task<RefreshToken> InsertRefreshTokenAsync(RefreshToken token);

    /// <summary>
    /// Look up a refresh token row by its SHA-256 hash (user eager-loaded).
    /// Returns null when unknown.
    /// </summary>
    Task<RefreshToken?> FindRefreshTokenByTokenHashAsync(string tokenHash);

    /// <summary>
    /// Atomic rotation: revoke <paramref name="revokedToken"/>, link it to
    /// <paramref name="replacementToken"/> via <c>ReplacedByTokenId</c> and persist
    /// the new token row in the same save.
    /// </summary>
    Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken revokedToken, RefreshToken replacementToken);

    /// <summary>Revoke every active refresh token belonging to the user (family revoke).</summary>
    Task RevokeAllActiveTokensForUserAsync(Guid userId, DateTime revokedAt);

    /// <summary>Flush any pending changes (e.g. token revocation on logout).</summary>
    Task SaveChangesAsync();
}