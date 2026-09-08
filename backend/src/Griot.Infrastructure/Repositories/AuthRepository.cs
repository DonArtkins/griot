using Griot.Application.Interfaces.Repositories;
using Griot.Domain.Entities;
using Griot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
        await _context.SaveChangesAsync().ConfigureAwait(false);
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

    public async Task<RefreshToken> RotateRefreshTokenAsync(RefreshToken revokedToken, RefreshToken replacementToken)
    {
        revokedToken.RevokedAt = DateTime.UtcNow;
        revokedToken.ReplacedByTokenId = replacementToken.Id;
        _context.RefreshTokens.Add(replacementToken);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return replacementToken;
    }

    public async Task RevokeAllActiveTokensForUserAsync(Guid userId, DateTime revokedAt)
    {
        var active = await _context.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (var token in active)
            token.RevokedAt = revokedAt;

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}