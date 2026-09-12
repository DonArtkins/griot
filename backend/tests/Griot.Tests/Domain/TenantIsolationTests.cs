using System;
using System.Linq;
using System.Threading.Tasks;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Griot.Application.Tenancy;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
using Moq;
using Xunit;

namespace Griot.Tests.Domain;

/// <summary>
/// Spec 29 acceptance: tenant isolation at the application layer.
/// (a) `CreateWorkspaceAsync` returns the owner's member user-data
/// (displayName/email/avatarUrl) exactly like GET does — the reported Postman
/// POST-vs-GET defect. (b) Writes require an active org scope (fail closed).
/// (c) Cross-tenant workspace access throws 403 cross-tenant.
/// </summary>
public class TenantIsolationTests
{
    private static readonly Guid OrgA = Guid.NewGuid();
    private static readonly Guid OrgB = Guid.NewGuid();
    private static readonly Guid OwnerId = Guid.NewGuid();

    private static DomainService BuildService(
        Guid orgScope,
        Mock<IGenericRepository<Workspace>>? workspaces,
        User? owner = null,
        Workspace? existing = null)
    {
        owner ??= new User
        {
            Id = OwnerId, Email = "owner@example.com",
            DisplayName = "Owner", AvatarUrl = "https://cdn.example/a.png"
        };
        var users = new Mock<IGenericRepository<User>>();
        users.Setup(r => r.GetByIdAsync(OwnerId)).ReturnsAsync(owner);

        var wsMock = workspaces ?? new Mock<IGenericRepository<Workspace>>();
        if (existing is not null)
            wsMock.Setup(r => r.GetByIdAsync(existing.Id)).ReturnsAsync(existing);

        var tenant = Mock.Of<ITenantContext>(t => t.OrganizationId == orgScope && t.IsOrganizationActive);
        return new DomainService(
            users.Object, wsMock.Object,
            Mock.Of<IGenericRepository<WorkspaceMember>>(),
            Mock.Of<IGenericRepository<Invite>>(),
            Mock.Of<IGenericRepository<Project>>(),
            Mock.Of<IGenericRepository<Board>>(),
            Mock.Of<IGenericRepository<Column>>(),
            Mock.Of<IGenericRepository<TaskItem>>(),
            Mock.Of<IGenericRepository<Comment>>(),
            Mock.Of<IGenericRepository<Attachment>>(),
            Mock.Of<IGenericRepository<Notification>>(),
            Mock.Of<IGenericRepository<ActivityLog>>(),
            Mock.Of<IGenericRepository<ErrorLog>>(),
            Mock.Of<IGenericRepository<AuditLog>>(),
            Mock.Of<IAuditService>(),
            tenant);
    }

    [Fact]
    public async Task CreateWorkspace_ReturnsOwnerMemberUserData_LikeGet()
    {
        var service = BuildService(OrgA, null);

        var dto = await service.CreateWorkspaceAsync(
            new CreateWorkspaceRequest { Name = "Acme", Slug = "acme" }, OwnerId);

        Assert.Equal(OrgA, dto.OrganizationId);
        var member = Assert.Single(dto.Members);
        Assert.Equal(OwnerId, member.UserId);
        Assert.Equal("Owner", member.DisplayName);
        Assert.Equal("owner@example.com", member.Email);
        Assert.Equal("https://cdn.example/a.png", member.AvatarUrl);
        Assert.Equal(WorkspaceRole.Owner, member.Role);
    }

    [Fact]
    public async Task CreateWorkspace_WithoutTenantScope_ThrowsForbidden()
    {
        var users = new Mock<IGenericRepository<User>>();
        var service = new DomainService(
            users.Object, Mock.Of<IGenericRepository<Workspace>>(),
            Mock.Of<IGenericRepository<WorkspaceMember>>(),
            Mock.Of<IGenericRepository<Invite>>(),
            Mock.Of<IGenericRepository<Project>>(),
            Mock.Of<IGenericRepository<Board>>(),
            Mock.Of<IGenericRepository<Column>>(),
            Mock.Of<IGenericRepository<TaskItem>>(),
            Mock.Of<IGenericRepository<Comment>>(),
            Mock.Of<IGenericRepository<Attachment>>(),
            Mock.Of<IGenericRepository<Notification>>(),
            Mock.Of<IGenericRepository<ActivityLog>>(),
            Mock.Of<IGenericRepository<ErrorLog>>(),
            Mock.Of<IGenericRepository<AuditLog>>(),
            Mock.Of<IAuditService>(),
            Mock.Of<ITenantContext>(t => t.OrganizationId == null));

        var ex = await Assert.ThrowsAsync<DomainError>(() =>
            service.CreateWorkspaceAsync(new CreateWorkspaceRequest { Name = "X" }, OwnerId));
        Assert.Equal(DomainErrorKind.Forbidden, ex.Kind);
    }

    [Fact]
    public async Task GetWorkspace_CrossTenant_ThrowsForbidden()
    {
        var foreign = new Workspace { Id = Guid.NewGuid(), OwnerId = OwnerId, OrganizationId = OrgB };
        var service = BuildService(OrgA, null, existing: foreign);

        var ex = await Assert.ThrowsAsync<DomainError>(() =>
            service.GetWorkspaceAsync(foreign.Id, OwnerId));
        Assert.Equal(DomainErrorKind.Forbidden, ex.Kind);
        Assert.Contains("cross-tenant", ex.Message);
    }

    [Fact]
    public void TenantGuard_AssertTenant_MismatchedOrg_Throws()
    {
        var tenant = Mock.Of<ITenantContext>(t => t.OrganizationId == OrgA && t.IsOrganizationActive);
        var ex = Assert.Throws<DomainError>(() => TenantGuard.AssertTenant(tenant, OrgB));
        Assert.Equal(DomainErrorKind.Forbidden, ex.Kind);
    }

    [Fact]
    public void TenantGuard_RequireOrganization_NullScope_Throws()
    {
        var tenant = Mock.Of<ITenantContext>(t => t.OrganizationId == null);
        Assert.Throws<DomainError>(() => TenantGuard.RequireOrganization(tenant));
    }

    [Fact]
    public void TenantGuard_RequireOrganization_InactiveOrg_ThrowsOrgSuspended()
    {
        // Spec 32/39 suspend gate (CodeRabbit fix): an inactive scoped org blocks ALL
        // tenant-scoped writes with the `org_suspended` response.
        var tenant = Mock.Of<ITenantContext>(t => t.OrganizationId == OrgA && !t.IsOrganizationActive);
        var ex = Assert.Throws<DomainError>(() => TenantGuard.RequireOrganization(tenant));
        Assert.Equal(DomainErrorKind.Forbidden, ex.Kind);
        Assert.Equal("org_suspended", ex.Message);
    }

    [Fact]
    public void TenantContext_WithTenantScope_RestoresPrevious()
    {
        var ctx = new TenantContext();
        TenantContext.SetCurrent(OrgA);
        try
        {
            ctx.WithTenantScope(OrgB, () => Assert.Equal(OrgB, ctx.OrganizationId));
            Assert.Equal(OrgA, ctx.OrganizationId);
        }
        finally
        {
            TenantContext.SetCurrent(null);
        }
    }
}
