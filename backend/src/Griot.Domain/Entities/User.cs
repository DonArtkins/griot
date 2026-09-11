using System;
using System.Collections.Generic;

namespace Griot.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public virtual Griot.Domain.Enums.TwoFactorMethod TwoFactorMethod { get; set; } = Griot.Domain.Enums.TwoFactorMethod.None;

    /// <summary>
    /// True once the user has verified the email address with a 6-digit OTP code
    /// delivered via Brevo (purpose `email_verify`). Registration sends the code
    /// automatically; `POST /api/auth/otp/verify` marks this true.
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Platform-level role (spec 29): User default, SuperAdmin = the operator
    /// of Griot itself (onboards/suspends/offboards companies). Distinct from
    /// the per-company <see cref="OrganizationMember"/> role.
    /// </summary>
    public virtual Griot.Domain.Enums.PlatformRole PlatformRole { get; set; } = Griot.Domain.Enums.PlatformRole.User;

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
    public virtual ICollection<OrganizationMember> OrganizationMembers { get; set; } = new List<OrganizationMember>();
    public virtual ICollection<Invite> SentInvites { get; set; } = new List<Invite>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<OtpChallenge> OtpChallenges { get; set; } = new List<OtpChallenge>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public virtual ICollection<ApiLog> ApiLogs { get; set; } = new List<ApiLog>();
    public virtual ICollection<ErrorLog> ErrorLogs { get; set; } = new List<ErrorLog>();
    public virtual ICollection<ErrorLog> SolvedErrors { get; set; } = new List<ErrorLog>();
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public virtual ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
