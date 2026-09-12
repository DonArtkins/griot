using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Griot.Application.DTOs.Organizations;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Griot.Application.Tenancy;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
using Griot.Infrastructure.Persistence;
using Griot.Infrastructure.Repositories;
using Griot.Tests.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Griot.Tests.Organizations;

/// <summary>
/// SQL suite for spec 32 (gated by GRIOT_RUN_SQL_TESTS=1 — mirrors RoleSqlTests):
/// proves the onboarding seed commits ALL rows together, that a mid-seed failure
/// leaves ZERO partial rows (real transaction rollback), the invite-accept flow,
/// suspend semantics + the TenantGuard org_suspended gate, and slug concurrency.
/// </summary>
public sealed class OrganizationLifecycleSqlTests : IClassFixture<SqlAuthFixture>
{
    private readonly SqlAuthFixture _fixture;
    public OrganizationLifecycleSqlTests(SqlAuthFixture fixture) => _fixture = fixture;

    private static Guid SuperAdminId = Guid.NewGuid();

    private async Task<User> SeedSuperAdminAsync()
    {
        await using var db = _fixture.CreateContext();
        var superAdmin = new User
        {
            Email = $"sa-{Guid.NewGuid():N}@example.com", DisplayName = "Platform SA", PasswordHash = "test"
        };
        db.Users.Add(superAdmin);
        await db.SaveChangesAsync();
        SuperAdminId = superAdmin.Id;
        return superAdmin;
    }

    private async Task<User> SeedOwnerAsync(string email)
    {
        await using var db = _fixture.CreateContext();
        var owner = new User { Email = email, DisplayName = "ACME Owner", PasswordHash = "test" };
        db.Users.Add(owner);
        await db.SaveChangesAsync();
        return owner;
    }

    private static Mock<ITenantContext> PlatformTenant()
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(t => t.OrganizationId).Returns((Guid?)null);
        tenant.SetupGet(t => t.IsSuperAdmin).Returns(true);
        tenant.SetupGet(t => t.IsOrganizationActive).Returns(true);
        return tenant;
    }

    private static OrganizationLifecycleService Service(GriotDbContext db, Mock<ITenantContext> tenant)
    {
        db.WithTenant(tenant.Object);
        // Audit rows persist in the SAME transaction as the mutation (spec 20 hard rule).
        var audit = new Mock<IAuditService>();
        audit.Setup(a => a.QueueAudit(It.IsAny<AuditEntry>())).Callback<AuditEntry>(entry =>
            db.AuditLogs.Add(new AuditLog
            {
                ActorId = entry.ActorId, OrganizationId = entry.OrganizationId,
                EntityType = entry.EntityType, EntityId = entry.EntityId, Action = entry.Action,
                Before = entry.Before, After = entry.After
            }));
        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SITE_URL"] = "https://test.griot" }).Build();
        return new OrganizationLifecycleService(new OrganizationLifecycleRepository(db), audit.Object,
            email.Object, configuration, NullLogger<OrganizationLifecycleService>.Instance, tenant.Object);
    }

    private static CreateOrganizationRequest Request(string slug, string ownerEmail) =>
        new("ACME Inc", slug, ownerEmail, "ACME Owner", OrganizationPlan.Pro);

    [SqlAuthFact]
    public async Task Onboard_SeedCommitsAllRowsTogether()
    {
        await SeedSuperAdminAsync();
        var slug = $"onboard-{Guid.NewGuid():N}"[..20];
        var owner = await SeedOwnerAsync($"owner-{Guid.NewGuid():N}@example.com");

        CreateOrganizationResponse result;
        await using (var db = _fixture.CreateContext())
        {
            var service = Service(db, PlatformTenant());
            result = await service.CreateAsync(SuperAdminId, Request(slug, owner.Email));
        }

        await using var verify = _fixture.CreateContext();
        var org = await verify.Organizations.IgnoreQueryFilters().FirstAsync(o => o.Slug == slug);
        Assert.Equal(OrganizationStatus.Active, org.Status);
        Assert.Equal(OrganizationPlan.Pro, org.PlanName);
        Assert.Equal(owner.Id, org.OwnerId);
        Assert.Equal(5, await verify.Roles.IgnoreQueryFilters().CountAsync(r => r.OrganizationId == org.Id && r.IsSystem));
        var workspace = await verify.Workspaces.IgnoreQueryFilters().FirstAsync(w => w.OrganizationId == org.Id);
        Assert.Equal($"{slug}-default", workspace.Slug);
        Assert.Equal(OrganizationMemberStatus.Invited,
            (await verify.OrganizationMembers.IgnoreQueryFilters().FirstAsync(m => m.OrganizationId == org.Id)).Status);
        Assert.Equal(1, await verify.OrganizationLifecycleEvents.IgnoreQueryFilters().CountAsync(e => e.OrganizationId == org.Id && e.Kind == "Onboarded"));
        Assert.Equal(1, await verify.AuditLogs.IgnoreQueryFilters().CountAsync(a => a.EntityId == org.Id && a.Action == "Organization.Onboarded"));
        Assert.False(string.IsNullOrWhiteSpace(result.OwnerInvite!.Token)); // token travels in the 201 payload
    }

    [SqlAuthFact]
    public async Task Onboard_MidSeedFailure_LeavesZeroPartialRows()
    {
        await SeedSuperAdminAsync();
        var slug = $"fail-{Guid.NewGuid():N}"[..18];
        var owner = await SeedOwnerAsync($"owner-{Guid.NewGuid():N}@example.com");

        // Pre-insert a workspace occupying the unique default slug for ANOTHER company:
        // the seed then fails on the unique index MID-transaction, after the org/roles/
        // member/invite/lifecycle rows were queued — proving the rollback leaves zero rows.
        await using (var setup = _fixture.CreateContext())
        {
            var blocker = new User { Email = $"blocker-{Guid.NewGuid():N}@example.com", PasswordHash = "test" };
            var blockerOrg = new Organization { Name = "Blocker", Slug = $"block-{Guid.NewGuid():N}"[..18], OwnerId = blocker.Id };
            setup.Users.Add(blocker);
            setup.Organizations.Add(blockerOrg);
            await setup.SaveChangesAsync();
            setup.Workspaces.Add(new Workspace
            {
                Name = "Blocker default", Slug = $"{slug}-default", OwnerId = blocker.Id, OrganizationId = blockerOrg.Id
            });
            await setup.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateContext())
        {
            var service = Service(db, PlatformTenant());
            await Assert.ThrowsAnyAsync<Exception>(
                () => service.CreateAsync(SuperAdminId, Request(slug, owner.Email)));
        }

        await using var verify = _fixture.CreateContext();
        Assert.False(await verify.Organizations.IgnoreQueryFilters().AnyAsync(o => o.Slug == slug));
        Assert.False(await verify.OrganizationMembers.IgnoreQueryFilters().AnyAsync(m => m.User.Email == owner.Email));
        Assert.False(await verify.OrganizationInvites.IgnoreQueryFilters().AnyAsync(i => i.Email == owner.Email));
        Assert.False(await verify.Workspaces.IgnoreQueryFilters().AnyAsync(w => w.Slug == $"{slug}-default" && w.Organization.Name == "ACME Inc"));
        Assert.False(await verify.OrganizationLifecycleEvents.IgnoreQueryFilters()
            .AnyAsync(e => e.Organization.Slug == slug));
        Assert.False(await verify.AuditLogs.IgnoreQueryFilters()
            .AnyAsync(a => a.Action == "Organization.Onboarded" && a.After!.Contains(slug)));
    }

    [SqlAuthFact]
    public async Task Onboard_DuplicateSlug_SerializesToOneConflict()
    {
        await SeedSuperAdminAsync();
        var slug = $"dup-{Guid.NewGuid():N}"[..16];
        var owner = await SeedOwnerAsync($"owner-{Guid.NewGuid():N}@example.com");

        await using var first = _fixture.CreateContext();
        await using var second = _fixture.CreateContext();
        var firstResult = await Service(first, PlatformTenant()).CreateAsync(SuperAdminId, Request(slug, owner.Email));
        var secondError = await Assert.ThrowsAsync<DomainError>(
            () => Service(second, PlatformTenant()).CreateAsync(SuperAdminId, Request(slug, owner.Email)));

        Assert.Equal(DomainErrorKind.Conflict, secondError.Kind);
        Assert.Equal(firstResult.Organization.Slug, slug);
        await using var verify = _fixture.CreateContext();
        Assert.Equal(1, await verify.Organizations.IgnoreQueryFilters().CountAsync(o => o.Slug == slug));
    }

    [SqlAuthFact]
    public async Task Suspend_ThenTenantWriteGate_FailsClosedWithOrgSuspended()
    {
        await SeedSuperAdminAsync();
        var slug = $"sus-{Guid.NewGuid():N}"[..16];
        var owner = await SeedOwnerAsync($"owner-{Guid.NewGuid():N}@example.com");
        Guid orgId;

        await using (var db = _fixture.CreateContext())
        {
            var service = Service(db, PlatformTenant());
            var result = await service.CreateAsync(SuperAdminId, Request(slug, owner.Email));
            orgId = result.Organization.Id;
            await service.SuspendAsync(SuperAdminId, orgId, new SuspendRequest("chargeback"));
        }

        await using var verify = _fixture.CreateContext();
        var suspended = await verify.Organizations.IgnoreQueryFilters().FirstAsync(o => o.Id == orgId);
        Assert.Equal(OrganizationStatus.Suspended, suspended.Status);
        Assert.Equal(1, await verify.OrganizationLifecycleEvents.IgnoreQueryFilters().CountAsync(e => e.OrganizationId == orgId && e.Kind == "Suspended"));
        Assert.Equal(1, await verify.AuditLogs.IgnoreQueryFilters().CountAsync(a => a.EntityId == orgId && a.Action == "Organization.Suspended"));

        // The spec-29/32 gate: a tenant scoped to the suspended organization fails
        // closed on writes with 403 org_suspended while reads/auth stay allowed.
        var suspendedTenant = new Mock<ITenantContext>();
        suspendedTenant.SetupGet(t => t.OrganizationId).Returns((Guid?)orgId);
        suspendedTenant.SetupGet(t => t.IsOrganizationActive).Returns(false);
        var gateError = Assert.Throws<DomainError>(() => TenantGuard.RequireOrganization(suspendedTenant.Object));
        Assert.Equal("org_suspended", gateError.Message);

        // Reactivate restores writes.
        await using (var db = _fixture.CreateContext())
        {
            await Service(db, PlatformTenant()).ReactivateAsync(SuperAdminId, orgId, null);
        }
        await using var restored = _fixture.CreateContext();
        Assert.Equal(OrganizationStatus.Active, (await restored.Organizations.IgnoreQueryFilters().FirstAsync(o => o.Id == orgId)).Status);
    }

    [SqlAuthFact]
    public async Task InviteAccept_EndToEnd_ActivatesOwner_SecondAccept_Conflicts()
    {
        await SeedSuperAdminAsync();
        var slug = $"acc-{Guid.NewGuid():N}"[..16];
        var ownerEmail = $"owner-{Guid.NewGuid():N}@example.com";
        var owner = await SeedOwnerAsync(ownerEmail);
        string token;
        Guid orgId;

        await using (var db = _fixture.CreateContext())
        {
            var service = Service(db, PlatformTenant());
            var result = await service.CreateAsync(SuperAdminId, Request(slug, ownerEmail));
            token = result.OwnerInvite!.Token;
            orgId = result.Organization.Id;
        }

        await using (var acceptDb = _fixture.CreateContext())
        {
            await Service(acceptDb, PlatformTenant()).AcceptInviteAsync(token, owner.Id);
        }

        await using var verify = _fixture.CreateContext();
        var member = await verify.OrganizationMembers.IgnoreQueryFilters().FirstAsync(m => m.OrganizationId == orgId && m.UserId == owner.Id);
        Assert.Equal(OrganizationMemberStatus.Active, member.Status);
        Assert.Equal(OrganizationRole.Owner, member.Role);
        var invite = await verify.OrganizationInvites.IgnoreQueryFilters().FirstAsync(i => i.OrganizationId == orgId);
        Assert.NotNull(invite.AcceptedAt);
        Assert.Equal(1, await verify.OrganizationLifecycleEvents.IgnoreQueryFilters().CountAsync(e => e.OrganizationId == orgId));
        Assert.Equal(1, await verify.AuditLogs.IgnoreQueryFilters().CountAsync(a => a.EntityId == invite.Id && a.Action == "Organization.InviteAccepted"));

        // Second accept (same token) conflicts under the row lock.
        await using (var again = _fixture.CreateContext())
        {
            var error = await Assert.ThrowsAsync<DomainError>(
                () => Service(again, PlatformTenant()).AcceptInviteAsync(token, owner.Id));
            Assert.Equal(DomainErrorKind.NotFound, error.Kind); // consumed invite no longer resolves
        }

        // The owner can now see the default workspace (lands in the company).
        Assert.True(await verify.Workspaces.IgnoreQueryFilters().AnyAsync(w => w.OrganizationId == orgId));
    }

    [SqlAuthFact]
    public async Task InviteAccept_EmailMismatch_Forbidden()
    {
        await SeedSuperAdminAsync();
        var slug = $"mis-{Guid.NewGuid():N}"[..16];
        var owner = await SeedOwnerAsync($"owner-{Guid.NewGuid():N}@example.com");
        var impostor = await SeedOwnerAsync($"imposter-{Guid.NewGuid():N}@example.com");
        string token;

        await using (var db = _fixture.CreateContext())
        {
            var result = await Service(db, PlatformTenant()).CreateAsync(SuperAdminId, Request(slug, owner.Email));
            token = result.OwnerInvite!.Token;
        }

        await using (var db = _fixture.CreateContext())
        {
            var error = await Assert.ThrowsAsync<DomainError>(
                () => Service(db, PlatformTenant()).AcceptInviteAsync(token, impostor.Id));
            Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
        }
    }

    [SqlAuthFact]
    public async Task Transfer_EndToEnd_RewritesOwnerAndDemotesPrevious()
    {
        await SeedSuperAdminAsync();
        var slug = $"tr-{Guid.NewGuid():N}"[..16];
        var owner = await SeedOwnerAsync($"owner-{Guid.NewGuid():N}@example.com");
        var nextOwner = await SeedOwnerAsync($"next-{Guid.NewGuid():N}@example.com");
        Guid orgId;

        await using (var db = _fixture.CreateContext())
        {
            var service = Service(db, PlatformTenant());
            var result = await service.CreateAsync(SuperAdminId, Request(slug, owner.Email));
            await service.AcceptInviteAsync(result.OwnerInvite!.Token, owner.Id);
            orgId = result.Organization.Id;
            // Promote nextOwner to an Active member first (Admin, as invited by SA).
            await using var promote = _fixture.CreateContext();
            promote.OrganizationMembers.Add(new OrganizationMember
            {
                OrganizationId = orgId, UserId = nextOwner.Id, Role = OrganizationRole.Admin,
                Status = OrganizationMemberStatus.Active
            });
            await promote.SaveChangesAsync();
        }

        await using (var db = _fixture.CreateContext())
        {
            await Service(db, PlatformTenant()).TransferOwnershipAsync(
                SuperAdminId, orgId, new TransferOwnershipRequest(nextOwner.Id));
        }

        await using var verify = _fixture.CreateContext();
        var transferred = await verify.Organizations.IgnoreQueryFilters().FirstAsync(o => o.Id == orgId);
        Assert.Equal(nextOwner.Id, transferred.OwnerId);
        var previous = await verify.OrganizationMembers.IgnoreQueryFilters().FirstAsync(m => m.OrganizationId == orgId && m.UserId == owner.Id);
        Assert.Equal(OrganizationRole.Admin, previous.Role);
        var newOwner = await verify.OrganizationMembers.IgnoreQueryFilters().FirstAsync(m => m.OrganizationId == orgId && m.UserId == nextOwner.Id);
        Assert.Equal(OrganizationRole.Owner, newOwner.Role);
        Assert.Equal(1, await verify.OrganizationLifecycleEvents.IgnoreQueryFilters().CountAsync(e => e.OrganizationId == orgId && e.Kind == "OwnershipTransferred"));
    }
}
