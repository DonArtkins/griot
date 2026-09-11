using Griot.Application.Interfaces.Repositories;
using Griot.Application.Services;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
using Griot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Griot.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAuthRepository"/>.
/// Persistence only — no token or hashing policy lives here.
/// </summary>
public sealed class AuthRepository : IAuthRepository
{
    private readonly GriotDbContext _context;

    public AuthRepository(GriotDbContext context)
    {
        _context = context;
    }

    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email).ConfigureAwait(false);
    }

    public async Task<User?> FindUserByEmailAsync(string email)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email)
            .ConfigureAwait(false);
    }

    public async Task<User> CreateUserAsync(User user)
    {
        _context.Users.Add(user);
        try
        {
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException sqlException &&
            sqlException.Errors.Cast<SqlError>().Any(error =>
                error.Number is 2601 or 2627 &&
                error.Message.Contains("'IX_Users_Email'", StringComparison.Ordinal) &&
                error.Message.Contains("'dbo.Users'", StringComparison.Ordinal)))
        {
            // The pre-check can lose a race; only the email index maps to HTTP 409.
            _context.Entry(user).State = EntityState.Detached;
            throw new DuplicateEmailException("Email is already registered.");
        }
        return user;
    }

    public async Task<RefreshToken> InsertRefreshTokenAsync(RefreshToken token)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return token;
    }

    public async Task<RefreshToken?> FindRefreshTokenByTokenHashAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash)
            .ConfigureAwait(false);
    }

    public async Task<RefreshToken?> RotateRefreshTokenAsync(RefreshToken revokedToken, RefreshToken replacementToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
        var affected = await _context.RefreshTokens
            .Where(r => r.Id == revokedToken.Id && r.UserId == revokedToken.UserId &&
                        r.FamilyId == revokedToken.FamilyId && r.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(r => r.RevokedAt, DateTime.UtcNow)
                .SetProperty(r => r.ReplacedByTokenId, replacementToken.Id))
            .ConfigureAwait(false);

        if (affected != 1)
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            return null;
        }

        // ExecuteUpdate bypasses tracking; do not mark the stale original entity modified.
        _context.RefreshTokens.Add(replacementToken);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        await transaction.CommitAsync().ConfigureAwait(false);
        return replacementToken;
    }

    public async Task RevokeFamilyAsync(Guid userId, Guid familyId, DateTime revokedAt)
    {
        await _context.RefreshTokens
            .Where(r => r.UserId == userId && r.FamilyId == familyId && r.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.RevokedAt, revokedAt))
            .ConfigureAwait(false);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task<OtpChallenge?> FindActiveOtpChallengeAsync(Guid userId, string purpose)
    {
        return await _context.OtpChallenges
            .AsNoTracking()
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.Consumed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task InsertOtpChallengeAsync(OtpChallenge challenge)
    {
        _context.OtpChallenges.Add(challenge);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task InvalidateOtpChallengesAsync(Guid userId, string purpose)
    {
        await _context.OtpChallenges
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.Consumed)
            .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.Consumed, true))
            .ConfigureAwait(false);
    }

    public async Task MarkOtpChallengeConsumedAsync(Guid id)
    {
        await _context.OtpChallenges
            .Where(o => o.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(o => o.Consumed, true))
            .ConfigureAwait(false);
    }

    public async Task AddOtpAttemptAsync(Guid id)
    {
        await _context.OtpChallenges
            .Where(o => o.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.AttemptCount, o => o.AttemptCount + 1))
            .ConfigureAwait(false);
    }

    public async Task MarkEmailVerifiedAsync(Guid userId)
    {
        await _context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.EmailVerified, true))
            .ConfigureAwait(false);
    }

    // ── Spec 30 (Auth & JWT v2): organization session + SuperAdmin bootstrap ──

    public async Task<Organization?> FindOrganizationByIdAsync(Guid organizationId)
    {
        return await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<OrganizationMember>> GetActiveOrganizationMembershipsAsync(Guid userId)
    {
        return await _context.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.CustomRole)
            .Include(m => m.Organization)
            .Where(m => m.UserId == userId
                        && m.Status == OrganizationMemberStatus.Active
                        && m.Organization.Status == OrganizationStatus.Active)
            .OrderByDescending(m => m.JoinedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<OrganizationMember?> FindActiveOrganizationMemberAsync(Guid organizationId, Guid userId)
    {
        return await _context.OrganizationMembers
            .AsNoTracking()
            .Include(m => m.CustomRole)
            .Include(m => m.Organization)
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId
                                      && m.UserId == userId
                                      && m.Status == OrganizationMemberStatus.Active
                                      && m.Organization.Status == OrganizationStatus.Active)
            .ConfigureAwait(false);
    }


    public async Task AddUserAsync(User user)
    {
        _context.Users.Add(user);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task<User?> FindUserByIdAsync(Guid userId)
    {
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId)
            .ConfigureAwait(false);
    }

    public async Task SetPlatformRoleAsync(Guid userId, PlatformRole role)
    {
        await _context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.PlatformRole, role)
                .SetProperty(u => u.UpdatedAt, DateTime.UtcNow))
            .ConfigureAwait(false);
    }
}
