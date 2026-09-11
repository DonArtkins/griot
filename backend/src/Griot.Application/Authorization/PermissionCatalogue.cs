using System;
using System.Collections.Generic;
using System.Linq;
using Griot.Domain.Enums;

namespace Griot.Application.Authorization;

/// <summary>
/// Spec 30/31: the fixed platform permission catalogue (canonical list:
/// <c>docs/multi-tenancy/MULTI-TENANCY-GUIDE.md</c> §3). The `perms` JWT v2 claim
/// is composed ONLY from these keys — unknown keys never reach a token, and
/// custom roles (spec 31) compose from the same catalogue. Ownership note: the
/// role→permission engine (CRUD, enforcement, validation of custom-role keys)
/// is spec 31; this catalogue only defines the key space and the system-role
/// mapping needed to stamp spec 30 tokens.
/// </summary>
public static class PermissionCatalogue
{
    public const string OrgRead = "org.read";
    public const string OrgSettingsManage = "org.settings.manage";
    public const string OrgMembersManage = "org.members.manage";
    public const string OrgRolesManage = "org.roles.manage";
    public const string OrgProjectsViewAll = "org.projects.view_all";
    public const string ProjectManage = "project.manage";
    public const string TaskManage = "task.manage";
    public const string CommentWrite = "comment.write";
    public const string ClientManage = "client.manage";
    public const string ClientFeedbackRead = "client.feedback.read";
    public const string ClientFeedbackRespond = "client.feedback.respond";
    public const string ReportGenerate = "report.generate";

    /// <summary>
    /// Reserved key (tier rules owned by spec 25). The key name exists so custom-role
    /// composition and the future enforcement engine can reference it, but spec 30
    /// never stamps it on a system-role token — spec 31 gates it behind a custom role.
    /// </summary>
    public const string LogReadTier = "log.read_tier";

    /// <summary>Keys reserved for later specs — never stamped on a system-role token by spec 30.</summary>
    public static readonly string[] Reserved = { LogReadTier };

    /// <summary>Stammable catalogue keys for spec 30 system roles (Reserved excluded).</summary>
    public static readonly string[] All =
    {
        OrgRead, OrgSettingsManage, OrgMembersManage, OrgRolesManage, OrgProjectsViewAll,
        ProjectManage, TaskManage, CommentWrite, ClientManage,
        ClientFeedbackRead, ClientFeedbackRespond, ReportGenerate
    };

    /// <summary>True when the key is part of the catalogue (including reserved keys).</summary>
    public static bool IsKnown(string key) =>
        All.Contains(key, StringComparer.Ordinal) || Reserved.Contains(key, StringComparer.Ordinal);

    /// <summary>
    /// Space-separated permission keys for a system <see cref="OrganizationRole"/>
    /// (spec 30 stamping map; guides §3). Custom roles resolve their own set from the
    /// <c>Roles.Permissions</c> column instead — spec 31 owns their CRUD/validation.
    /// </summary>
    public static string PermsForSystemRole(OrganizationRole role) => role switch
    {
        OrganizationRole.Owner => Join(All),
        OrganizationRole.Admin => Join(All),
        OrganizationRole.ProjectManager => Join(new[]
        {
            OrgRead, ProjectManage, TaskManage, CommentWrite,
            ClientManage, ClientFeedbackRead, ClientFeedbackRespond, ReportGenerate
        }),
        OrganizationRole.Member => string.Empty,
        OrganizationRole.Client => Join(new[] { OrgRead, ClientFeedbackRead }),
        OrganizationRole.Custom => string.Empty, // callers must resolve the Role.Permissions column
        _ => string.Empty
    };

    /// <summary>Deterministic space-separated join (deduplicated, ordinal sort).</summary>
    public static string Join(IEnumerable<string> keys) =>
        string.Join(" ", keys.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().OrderBy(k => k, StringComparer.Ordinal));
}