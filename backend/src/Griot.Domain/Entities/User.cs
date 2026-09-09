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
    public Griot.Domain.Enums.TwoFactorMethod TwoFactorMethod { get; set; } = Griot.Domain.Enums.TwoFactorMethod.None;

    /// <summary>
    /// True once the user has verified the email address with a 6-digit OTP code
    /// delivered via Brevo (purpose `email_verify`). Registration sends the code
    /// automatically; `POST /api/auth/otp/verify` marks this true.
    /// </summary>
    public bool EmailVerified { get; set; }

    public ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
    public ICollection<Invite> SentInvites { get; set; } = new List<Invite>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<OtpChallenge> OtpChallenges { get; set; } = new List<OtpChallenge>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<ApiLog> ApiLogs { get; set; } = new List<ApiLog>();
    public ICollection<ErrorLog> ErrorLogs { get; set; } = new List<ErrorLog>();
    public ICollection<ErrorLog> SolvedErrors { get; set; } = new List<ErrorLog>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
