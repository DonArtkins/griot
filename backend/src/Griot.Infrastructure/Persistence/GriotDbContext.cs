using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
using Griot.Application.Tenancy;

namespace Griot.Infrastructure.Persistence;

public class GriotDbContext : DbContext
{
        public GriotDbContext(DbContextOptions<GriotDbContext> options) : base(options) { }

    /// <summary>
    /// Spec 29 — per-context tenant captured for the global query filters.
    /// IMPORTANT (EF model cache): the SAME compiled model is shared across
    /// all context instances, but the filter parameterizes on this FIELD, so
    /// each context instance evaluates its own value at query time (standard
    /// EF Core pattern — see ApplyTenantQueryFilters). Set immediately after
    /// constructing the context (middleware sets it per request; tests set it
    /// directly or via <see cref="TenantContext"/>).
    /// </summary>
    public Guid? TenantId
    {
        get => _tenantId;
        set => _tenantId = value;
    }

    private Guid? _tenantId;

    /// <summary>
    /// Spec 29: SuperAdmin platform flag (spec 32/33). When true, strict-tenant
    /// global filters are bypassed so the operator can list/manage every company.
    /// Set only via <see cref="TenantDbContextExtensions.WithTenant"/> from the
    /// middleware-resolved ITenantContext — never from caller input.
    /// </summary>
    public bool IsSuperAdmin { get; set; }

    // NOTE: OnConfiguring is intentionally NOT overridden. Lazy-loading proxies are
    // configured exclusively through DI (AddPooledDbContextFactory / design-time factory).
    // Calling UseLazyLoadingProxies() inside OnConfiguring throws
    // "OnConfiguring cannot be used to modify DbContextOptions when DbContext
    //  pooling is enabled" — the pooled factory is used by runtime + DataLoaders.

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<OrganizationInvite> OrganizationInvites => Set<OrganizationInvite>();
    public DbSet<OrganizationLifecycleEvent> OrganizationLifecycleEvents => Set<OrganizationLifecycleEvent>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Invite> Invites => Set<Invite>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<Column> Columns => Set<Column>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<ApiLog> ApiLogs => Set<ApiLog>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users
        modelBuilder.Entity<User>(b =>
        {
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.Email).HasMaxLength(320);
            b.Property(u => u.DisplayName).HasMaxLength(100);
            b.Property(u => u.AvatarUrl).HasMaxLength(500);
            b.Property(u => u.PasswordHash).HasMaxLength(512);
            b.Property(u => u.TwoFactorMethod).HasConversion<string>();
            // Spec 29: platform role on the user (global — users span companies).
            b.Property(u => u.PlatformRole).HasConversion<string>();
        });

        // Organizations — the tenant root (spec 29, Pool model).
        modelBuilder.Entity<Organization>(b =>
        {
            b.HasIndex(o => o.Slug).IsUnique();
            b.HasIndex(o => o.OwnerId);
            b.Property(o => o.Name).HasMaxLength(150);
            b.Property(o => o.Slug).HasMaxLength(100);
            b.Property(o => o.Status).HasConversion<string>();
            // Amendment v2 `Plan` is a T-SQL reserved word, so the Organizations plan
            // property maps to column PlanName; docs/ERD keep calling the field Plan.
            b.Property(o => o.PlanName).HasColumnName("PlanName").HasConversion<string>();

            b.HasOne(o => o.Owner)
                .WithMany()
                .HasForeignKey(o => o.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrganizationMembers
        modelBuilder.Entity<OrganizationMember>(b =>
        {
            // One membership row per user per company (spec 29).
            b.HasIndex(m => new { m.OrganizationId, m.UserId }).IsUnique();
            b.HasIndex(m => m.UserId);
            b.Property(m => m.Role).HasConversion<string>();
            b.Property(m => m.Status).HasConversion<string>();

            b.HasOne(m => m.Organization)
                .WithMany(o => o.Members)
                .HasForeignKey(m => m.OrganizationId)
                // Org FKs are NO ACTION (Restrict) — cascade from Organizations to both
                // Workspaces and Projects creates multiple cascade paths (SQL error 1785).
                // Org removal is app-managed: the spec-33 offboarding purge deletes
                // children explicitly in leaves-first dependency order, one transaction
                // per org-scoped chunk with a resumable OrganizationLifecycleEvents
                // progress checkpoint (spec 33 + spec-41 revision).
                .OnDelete(DeleteBehavior.Restrict);

            // NOTE (spec 29 / 33): User side is Restrict, not Cascade — a user row
            // must never vanish implicitly; the offboarding purge (spec 33)
            // removes memberships explicitly (tombstone pattern). Restrict also
            // avoids converging cascade paths on OrganizationMembers.
            b.HasOne(m => m.User)
                .WithMany(u => u.OrganizationMembers)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(m => m.CustomRole)
                .WithMany(r => r.Members)
                .HasForeignKey(m => m.CustomRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Roles (custom company roles + seeded system roles — spec 31)
        modelBuilder.Entity<Role>(b =>
        {
            b.HasIndex(r => r.OrganizationId);
            b.Property(r => r.Name).HasMaxLength(100);
            b.Property(r => r.Permissions).HasMaxLength(2048);

            b.HasOne(r => r.Organization)
                .WithMany(o => o.Roles)
                .HasForeignKey(r => r.OrganizationId)
                // Org FKs are NO ACTION — see the OrganizationMember note (error 1785).
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrganizationInvites
        modelBuilder.Entity<OrganizationInvite>(b =>
        {
            b.HasIndex(i => i.OrganizationId);
            b.HasIndex(i => i.Token).IsUnique();
            b.Property(i => i.Role).HasConversion<string>();
            b.Property(i => i.Status).HasConversion<string>();
            b.Property(i => i.Email).HasMaxLength(320);
            b.Property(i => i.Token).HasMaxLength(128);

            b.HasOne(i => i.Organization)
                .WithMany(o => o.Invites)
                .HasForeignKey(i => i.OrganizationId)
                // Org FKs are NO ACTION — see the OrganizationMember note (error 1785).
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(i => i.CustomRole)
                .WithMany(r => r.Invites)
                .HasForeignKey(i => i.CustomRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(i => i.InvitedBy)
                .WithMany()
                .HasForeignKey(i => i.InvitedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrganizationLifecycleEvents (spec 29 / 32 / 33)
        modelBuilder.Entity<OrganizationLifecycleEvent>(b =>
        {
            b.HasIndex(e => new { e.OrganizationId, e.CreatedAt });
            b.Property(e => e.Kind).HasMaxLength(30);

            b.HasOne(e => e.Organization)
                .WithMany(o => o.LifecycleEvents)
                .HasForeignKey(e => e.OrganizationId)
                // Org FKs are NO ACTION — see the OrganizationMember note (error 1785).
                .OnDelete(DeleteBehavior.Restrict);
        });

        ApplyTenantConfiguration(modelBuilder);
        ApplyTenantQueryFilters(modelBuilder);

        // --- Pre-existing entity configuration (unchanged below) ---

        // Workspaces
        modelBuilder.Entity<Workspace>(b =>
        {
            b.HasIndex(w => w.Slug).IsUnique();
            b.Property(w => w.Name).HasMaxLength(100);
            b.Property(w => w.Slug).HasMaxLength(100);

            b.HasOne(w => w.Owner)
                .WithMany()
                .HasForeignKey(w => w.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Spec 29: workspace -> organization. NO ACTION (Restrict), NOT cascade —
            // cascading Organizations to both Workspaces and Projects yields multiple
            // cascade paths (SQL Server error 1785: "may cause cycles or multiple
            // cascade paths"). Org removal is app-managed: the spec-33 offboarding
            // purge deletes tenant rows explicitly in dependency order; suspension
            // blocks writes without deleting.
            b.HasIndex(w => w.OrganizationId);
            b.HasOne(w => w.Organization)
                .WithMany(o => o.Workspaces)
                .HasForeignKey(w => w.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- Tenant column/index configuration (spec 29, Pool model) ---
        // NOT NULL OrganizationId on strict-tenant tables; nullable on the four
        // observability tables (null = platform-level event).
        modelBuilder.Entity<Project>(b => { b.HasIndex(p => p.OrganizationId); });
        modelBuilder.Entity<Board>(b => { b.HasIndex(x => x.OrganizationId); });
        modelBuilder.Entity<Column>(b => { b.HasIndex(x => x.OrganizationId); });
        modelBuilder.Entity<TaskItem>(b => { b.HasIndex(t => t.OrganizationId); });
        modelBuilder.Entity<Comment>(b => { b.HasIndex(c => c.OrganizationId); });
        modelBuilder.Entity<Attachment>(b => { b.HasIndex(a => a.OrganizationId); });
        modelBuilder.Entity<Invite>(b => { b.HasIndex(i => i.OrganizationId); });
        modelBuilder.Entity<Notification>(b => { b.HasIndex(n => n.OrganizationId); });
        modelBuilder.Entity<ActivityLog>(b => { b.HasIndex(a => a.OrganizationId); });
        modelBuilder.Entity<ApiLog>(b => { b.HasIndex(a => a.OrganizationId); });
        modelBuilder.Entity<ErrorLog>(b => { b.HasIndex(e => e.OrganizationId); });
        modelBuilder.Entity<AuditLog>(b => { b.HasIndex(a => a.OrganizationId); });


        // --- Pre-existing entity configuration (unchanged below; Workspaces already configured above) ---

        // WorkspaceMembers
        modelBuilder.Entity<WorkspaceMember>(b =>
        {
            b.HasKey(wm => new { wm.WorkspaceId, wm.UserId });
            b.Property(wm => wm.Role).HasConversion<string>();

            b.HasOne(wm => wm.Workspace)
                .WithMany(w => w.Members)
                .HasForeignKey(wm => wm.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(wm => wm.User)
                .WithMany(u => u.WorkspaceMembers)
                .HasForeignKey(wm => wm.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Invites
        modelBuilder.Entity<Invite>(b =>
        {
            b.HasIndex(i => i.Token).IsUnique();
            b.HasIndex(i => new { i.WorkspaceId, i.Email });
            
            b.Property(i => i.Role).HasConversion<string>();
            b.Property(i => i.Status).HasConversion<string>();
            b.Property(i => i.Email).HasMaxLength(320);
            b.Property(i => i.Token).HasMaxLength(128);

            b.HasOne(i => i.Workspace)
                .WithMany(w => w.Invites)
                .HasForeignKey(i => i.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(i => i.InvitedBy)
                .WithMany(u => u.SentInvites)
                .HasForeignKey(i => i.InvitedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Projects
        modelBuilder.Entity<Project>(b =>
        {
            b.Property(p => p.Name).HasMaxLength(150);
            b.Property(p => p.Key).HasMaxLength(10);
            b.Property(p => p.Status).HasConversion<string>();

            b.HasOne(p => p.Workspace)
                .WithMany(w => w.Projects)
                .HasForeignKey(p => p.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Spec 29: project -> organization, explicit NO ACTION (Restrict). Without
            // this EF's convention cascades from Organizations, which together with the
            // Workspace->Organization path gives Projects two cascade sources
            // (SQL Server error 1785). Project.OrganizationId is derived data owned by
            // the workspace chain; it is removed with the workspace cascade.
            b.HasOne(p => p.Organization)
                .WithMany()
                .HasForeignKey(p => p.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Boards
        modelBuilder.Entity<Board>(b =>
        {
            b.Property(b => b.Name).HasMaxLength(100);

            b.HasOne(b => b.Project)
                .WithMany(p => p.Boards)
                .HasForeignKey(b => b.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Columns
        modelBuilder.Entity<Column>(b =>
        {
            b.Property(c => c.Name).HasMaxLength(100);

            b.HasOne(c => c.Board)
                .WithMany(b => b.Columns)
                .HasForeignKey(c => c.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskItems
        modelBuilder.Entity<TaskItem>(b =>
        {
            b.HasIndex(t => new { t.ColumnId, t.Position });
            b.HasIndex(t => t.AssigneeId);
            b.HasIndex(t => t.DueDate);

            b.Property(t => t.Title).HasMaxLength(200);
            b.Property(t => t.Status).HasConversion<string>();
            b.Property(t => t.Priority).HasConversion<string>();
            b.Property(t => t.Position).HasColumnType("decimal(18,4)");

            // BoardId is denormalized off the mandatory ColumnId mapping: it is
            // mapped as a plain (non-null) column via the entity property.

            b.HasOne(t => t.Column)
                .WithMany(c => c.TaskItems)
                .HasForeignKey(t => t.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(t => t.Assignee)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssigneeId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(t => t.Creator)
                .WithMany(u => u.CreatedTasks)
                .HasForeignKey(t => t.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Comments
        modelBuilder.Entity<Comment>(b =>
        {
            b.HasOne(c => c.TaskItem)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Attachments
        modelBuilder.Entity<Attachment>(b =>
        {
            b.Property(a => a.FileName).HasMaxLength(255);
            b.Property(a => a.MimeType).HasMaxLength(100);
            b.Property(a => a.StorageUrl).HasMaxLength(500);

            b.HasOne(a => a.TaskItem)
                .WithMany(t => t.Attachments)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(a => a.Uploader)
                .WithMany(u => u.Attachments)
                .HasForeignKey(a => a.UploaderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ActivityLogs
        modelBuilder.Entity<ActivityLog>(b =>
        {
            b.HasIndex(a => new { a.WorkspaceId, a.CreatedAt }).IsDescending(false, true);

            b.Property(a => a.EntityType).HasMaxLength(50);
            b.Property(a => a.Action).HasMaxLength(50);

            b.HasOne(a => a.Workspace)
                .WithMany(w => w.ActivityLogs)
                .HasForeignKey(a => a.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
                
            b.HasOne(a => a.Actor)
                .WithMany(u => u.ActivityLogs)
                .HasForeignKey(a => a.ActorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Notifications
        modelBuilder.Entity<Notification>(b =>
        {
            b.HasIndex(n => new { n.UserId, n.ReadAt });
            
            b.Property(n => n.Type).HasConversion<string>();
            b.Property(n => n.Title).HasMaxLength(200);
            b.Property(n => n.Body).HasMaxLength(500);
            b.Property(n => n.TargetRef).HasMaxLength(200);

            b.HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshTokens
        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.HasIndex(r => r.UserId);
            b.HasIndex(r => r.TokenHash).IsUnique();
            // FamilyId index: scopes reuse-revocation queries to one rotation chain
            b.HasIndex(r => new { r.UserId, r.FamilyId });

            b.Property(r => r.TokenHash).HasMaxLength(128);

            b.HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ApiLogs
        modelBuilder.Entity<ApiLog>(b =>
        {
            b.HasIndex(a => a.RequestId).IsUnique();
            // ERD amendment (2026-09-11, spec 20): standalone CreatedAt index —
            // spillover-window and distinct-user incident queries scan by time alone.
            b.HasIndex(a => a.CreatedAt);
            b.HasIndex(a => new { a.UserId, a.CreatedAt });
            b.HasIndex(a => a.Path);

            b.Property(a => a.Method).HasMaxLength(8);
            b.Property(a => a.Path).HasMaxLength(300);
            b.Property(a => a.QueryString).HasMaxLength(500);
            b.Property(a => a.UserAgent).HasMaxLength(300);
            b.Property(a => a.IpAddress).HasMaxLength(45);

            b.HasOne(a => a.User)
                .WithMany(u => u.ApiLogs)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ErrorLogs
        modelBuilder.Entity<ErrorLog>(b =>
        {
            // ERD amendment (2026-09-11, spec 20): request-id lookup —
            // "one request, one incident" recipe (LOGGING-AUDIT-REPORT §5).
            b.HasIndex(e => e.RequestId);
            b.HasIndex(e => e.FixStatus).HasFilter("[FixStatus] IN ('Open', 'Investigating')");
            b.HasIndex(e => e.FixedAt);

            b.Property(e => e.FixStatus).HasConversion<string>();
            b.Property(e => e.ExceptionType).HasMaxLength(200);
            b.Property(e => e.Source).HasMaxLength(100);

            b.HasOne(e => e.User)
                .WithMany(u => u.ErrorLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.SolvedByUser)
                .WithMany(u => u.SolvedErrors)
                .HasForeignKey(e => e.SolvedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // AuditLogs
        modelBuilder.Entity<AuditLog>(b =>
        {
            b.HasIndex(a => a.ActorId);
            b.HasIndex(a => a.ActivityId);
            // ERD amendment (2026-09-11, spec 20): audit rows correlate to the
            // producing request; "what a mutation changed" incident recipe scans
            // entity + time (AuditLogs WHERE EntityType/EntityId ORDER BY CreatedAt).
            b.HasIndex(a => a.RequestId);
            b.HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt });

            b.Property(a => a.Action).HasMaxLength(50);
            b.Property(a => a.EntityType).HasMaxLength(50);

            b.HasOne(a => a.Actor)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.ActorId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(a => a.ActivityLog)
                .WithMany()
                .HasForeignKey(a => a.ActivityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OtpChallenges
        modelBuilder.Entity<OtpChallenge>(b =>
        {
            b.HasIndex(o => o.UserId);
            b.Property(o => o.CodeHash).HasMaxLength(128);
            b.Property(o => o.Purpose).HasMaxLength(50);
            b.Property(o => o.RequestIp).HasMaxLength(45);

            b.HasOne(o => o.User)
                .WithMany(u => u.OtpChallenges)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Reports
        modelBuilder.Entity<Report>(b =>
        {
            b.HasIndex(r => r.WorkspaceId);
            b.Property(r => r.Type).HasMaxLength(50);
            b.Property(r => r.GeneratedBy).HasMaxLength(100);

            b.HasOne(r => r.Workspace)
                .WithMany(w => w.Reports)
                .HasForeignKey(r => r.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }


    /// <summary>
    /// Spec 29: OrganizationId columns + indexes (Pool model). NOT NULL on
    /// strict-tenant tables; nullable on the four observability tables
    /// (null = platform-level event). The workspace -> organization FK is
    /// amendment-v2 Cascade (org removal is the spec-33 offboarding purge;
    /// suspension blocks writes without deleting, per spec 32).
    /// </summary>
    private void ApplyTenantConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(b => { b.HasIndex(p => p.OrganizationId); });
        modelBuilder.Entity<Board>(b => { b.HasIndex(x => x.OrganizationId); });
        modelBuilder.Entity<Column>(b => { b.HasIndex(x => x.OrganizationId); });
        modelBuilder.Entity<TaskItem>(b => { b.HasIndex(t => t.OrganizationId); });
        modelBuilder.Entity<Comment>(b => { b.HasIndex(c => c.OrganizationId); });
        modelBuilder.Entity<Attachment>(b => { b.HasIndex(a => a.OrganizationId); });
        modelBuilder.Entity<Invite>(b => { b.HasIndex(i => i.OrganizationId); });
        modelBuilder.Entity<Notification>(b => { b.HasIndex(n => n.OrganizationId); });
        modelBuilder.Entity<ActivityLog>(b => { b.HasIndex(a => a.OrganizationId); });
        modelBuilder.Entity<ApiLog>(b => { b.HasIndex(a => a.OrganizationId); });
        modelBuilder.Entity<ErrorLog>(b => { b.HasIndex(e => e.OrganizationId); });
        modelBuilder.Entity<AuditLog>(b => { b.HasIndex(a => a.OrganizationId); });
    }

    /// <summary>
    /// Spec 29 — tenant global query filters (Pool model; SQL Server has no
    /// RLS). The tenant id is captured per-context in the <c>_tenantId</c>
    /// field so EF can parameterize the filter. Strict-tenant entities return
    /// EMPTY when there is no active scope (fail closed for reads); the
    /// nullable-org observability tables additionally surface platform-level
    /// (null-org) rows. Writes are guarded separately by
    /// <see cref="Griot.Application.Tenancy.TenantGuard"/> (403).
    /// </summary>
    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        // Spec 29: SuperAdmin platform sessions (IsSuperAdmin, resolved from the
        // platform role before any org role) bypass strict-tenant filters so the
        // operator can list/manage every company (spec 32/33). Tenant sessions
        // stay fail-closed: no scope (or a foreign org) sees zero tenant rows.
        modelBuilder.Entity<Workspace>().HasQueryFilter(w =>
            IsSuperAdmin || (_tenantId != null && w.OrganizationId == _tenantId));
        modelBuilder.Entity<Project>().HasQueryFilter(p =>
            IsSuperAdmin || (_tenantId != null && p.OrganizationId == _tenantId));
        modelBuilder.Entity<Board>().HasQueryFilter(x =>
            IsSuperAdmin || (_tenantId != null && x.OrganizationId == _tenantId));
        modelBuilder.Entity<Column>().HasQueryFilter(x =>
            IsSuperAdmin || (_tenantId != null && x.OrganizationId == _tenantId));
        modelBuilder.Entity<TaskItem>().HasQueryFilter(t =>
            IsSuperAdmin || (_tenantId != null && t.OrganizationId == _tenantId));
        modelBuilder.Entity<Comment>().HasQueryFilter(c =>
            IsSuperAdmin || (_tenantId != null && c.OrganizationId == _tenantId));
        modelBuilder.Entity<Attachment>().HasQueryFilter(a =>
            IsSuperAdmin || (_tenantId != null && a.OrganizationId == _tenantId));
        modelBuilder.Entity<Invite>().HasQueryFilter(i =>
            IsSuperAdmin || (_tenantId != null && i.OrganizationId == _tenantId));
        modelBuilder.Entity<Notification>().HasQueryFilter(n =>
            IsSuperAdmin || (_tenantId != null && n.OrganizationId == _tenantId));
        // Amendment v2: ActivityLogs carry nullable OrganizationId (null = legacy
        // rows not yet backfilled / platform events), so the fail-closed read shape
        // stays consistent with the other three observability tables.
        modelBuilder.Entity<ActivityLog>().HasQueryFilter(a =>
            _tenantId == null || a.OrganizationId == null || a.OrganizationId == _tenantId);
        modelBuilder.Entity<ApiLog>().HasQueryFilter(a =>
            _tenantId == null || a.OrganizationId == null || a.OrganizationId == _tenantId);
        modelBuilder.Entity<ErrorLog>().HasQueryFilter(e =>
            _tenantId == null || e.OrganizationId == null || e.OrganizationId == _tenantId);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(a =>
            _tenantId == null || a.OrganizationId == null || a.OrganizationId == _tenantId);
        modelBuilder.Entity<OrganizationMember>().HasQueryFilter(m =>
            IsSuperAdmin || (_tenantId != null && m.OrganizationId == _tenantId));
        modelBuilder.Entity<Role>().HasQueryFilter(r =>
            IsSuperAdmin || (_tenantId != null && r.OrganizationId == _tenantId));
        modelBuilder.Entity<OrganizationInvite>().HasQueryFilter(i =>
            IsSuperAdmin || (_tenantId != null && i.OrganizationId == _tenantId));
        modelBuilder.Entity<OrganizationLifecycleEvent>().HasQueryFilter(e =>
            IsSuperAdmin || (_tenantId != null && e.OrganizationId == _tenantId));
        // Organizations have NO tenant filter in spec 29: they are the tenant
        // roots themselves. Read scoping (member sees own orgs, SuperAdmin sees
        // all) is enforced in the service layer, not by a global filter — a
        // membership-join filter here would self-reference filtered sets and
        // break the SuperAdmin platform path. Full org read API ships in spec 32.
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Properties.Any(p => p.Metadata.Name == "CreatedAt"))
                {
                    entry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                }
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                {
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Properties.Any(p => p.Metadata.Name == "UpdatedAt"))
                {
                    entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
                }
            }
        }
    }
}
