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
    /// <exception cref="Griot.Application.Services.DuplicateEmailException">The Users.Email unique index rejected a concurrent registration.</exception>
    Task<User> CreateUserAsync(User user);

    /// <summary>Persist a new (hashed) refresh token row and return it.</summary>
    Task<RefreshToken> InsertRefreshTokenAsync(RefreshToken token);

    /// <summary>
    /// Look up a refresh token row by its SHA-256 hash (user eager-loaded).
    /// Returns null when unknown.
    /// </summary>
    Task<RefreshToken?> FindRefreshTokenByTokenHashAsync(string tokenHash);

    /// <summary>
    /// Atomic rotation using a conditional UPDATE (RevokedAt IS NULL) inside a transaction.
    /// Returns the replacement token when exactly one row was affected (success).
    /// Returns null when zero rows were affected — the token was already revoked (race condition /
    /// concurrent refresh). Callers must treat null as a reuse attack.
    /// </summary>
    Task<RefreshToken?> RotateRefreshTokenAsync(RefreshToken revokedToken, RefreshToken replacementToken);

    /// <summary>
    /// Revoke every active refresh token belonging to <paramref name="userId"/>
    /// that shares the given <paramref name="familyId"/> (reuse attack — single rotation chain).
    /// Does NOT revoke tokens from other login sessions.
    /// </summary>
    Task RevokeFamilyAsync(Guid userId, Guid familyId, DateTime revokedAt);

    /// <summary>Flush any pending changes (e.g. token revocation on logout).</summary>
    Task SaveChangesAsync();

    /// <summary>The newest unconsumed, unexpired OTP challenge for (userId, purpose), or null.</summary>
    Task<OtpChallenge?> FindActiveOtpChallengeAsync(Guid userId, string purpose);

    /// <summary>Persist a new (hashed) OTP challenge row.</summary>
    Task InsertOtpChallengeAsync(OtpChallenge challenge);

    /// <summary>Mark every unconsumed challenge for (userId, purpose) consumed — a fresh code supersedes older ones.</summary>
    Task InvalidateOtpChallengesAsync(Guid userId, string purpose);

    /// <summary>Consume a challenge (success or lockout).</summary>
    Task MarkOtpChallengeConsumedAsync(Guid id);

    /// <summary>Record one more failed attempt on a challenge.</summary>
    Task AddOtpAttemptAsync(Guid id);

    /// <summary>Mark the user's email address as verified (OTP purpose `email_verify`).</summary>
    Task MarkEmailVerifiedAsync(Guid userId);
}
