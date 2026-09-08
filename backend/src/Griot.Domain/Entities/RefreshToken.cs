using System;

namespace Griot.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Stable identifier shared by an entire rotation chain (family).
    /// Set once at issuance; copied to every replacement so reuse detection
    /// can revoke only the tokens in the same chain — not all user sessions.
    /// </summary>
    public Guid FamilyId { get; set; } = Guid.NewGuid();

    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
