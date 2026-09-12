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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Griot.Tests.Organizations;

/// <summary>
/// Unit tests for the spec-32 onboarding/lifecycle engine. Transaction helpers run
/// their actions inline (mock), so the transactional-seed wiring is exercised here
/// and the REAL commit/rollback behavior is proven in <see cref="OrganizationLifecycleSqlTests"/>.
/// </summary>
public sealed class OrganizationLifecycleServiceTests
{
    private static readonly Guid SuperAdminId = Guid.NewGuid();

    private static Mock<ITenantContext> Tenant(bool isSuperAdmin = true)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(t => t.OrganizationId).Returns((Guid?)null);
        tenant.SetupGet(t => t.IsSuperAdmin).Returns(isSuperAdmin);
        tenant.SetupGet(t => t.IsOrganizationActive).Returns(true);
        return tenant;
    }

    private static Mock<IEmailService> Email(bool delivered = true)
    {
        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(delivered);
        return email;
    }

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SITE_URL"] = "https://test.griot"
        }).Build();

    private static Mock<IOrganizationLifecycleRepository> Repo(Action<Mock<IOrganizationLifecycleRepository>>? configure = null)
    {
        var repo = new Mock<IOrganizationLifecycleRepository>();
        // Transaction helpers run their actions inline (unit-level wiring; real SQL
        // commit/rollback is proven in the SQL suite).
        repo.Setup(r => r.ExecuteOnboardingTransactionAsync(It.IsAny<string>(), It.IsAny<Func<Task>>()))
            .Returns<string, Func<Task>>((_, action) => action());
        repo.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Guid>(), It.IsAny<Func<Task>>()))
            .Returns<Guid, Func<Task>>((_, action) => action());
        repo.Setup(r => r.ExecuteInTransactionAsync(It.IsAny<Guid>(), It.IsAny<Func<Task<OrganizationDto>>>()))
            .Returns<Guid, Func<Task<OrganizationDto>>>((_, action) => action());
        configure?.Invoke(repo);
        return repo;
    }

    private static OrganizationLifecycleService Service(
        Mock<IOrganizationLifecycleRepository> repo, Mock<ITenantContext> tenant, Mock<IEmailService>? email = null,
        Mock<IAuditService>? audit = null)
        => new(repo.Object, (audit ?? new Mock<IAuditService>()).Object, (email ?? Email()).Object,
            Configuration(), NullLogger<OrganizationLifecycleService>.Instance, tenant.Object);

    private static CreateOrganizationRequest Request(string slug = "acme", string ownerEmail = "owner@example.com") =>
        new("ACME Inc", slug, ownerEmail, OrganizationPlan.Pro);

    // ── Onboarding ──

    [Fact]
    public async Task Create_Happy_SeedsFullPayload_SendsInvite_Returns201Payload()
    {
        var owner = new User { Id = Guid.NewGuid(), Email = "owner@example.com", DisplayName = "ACME Owner" };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindUserByEmailAsync("owner@example.com")).ReturnsAsync(owner);
            r.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);
        });
        var email = Email();
        var service = Service(repo, Tenant(), email);

        var result = await service.CreateAsync(SuperAdminId, Request());

        Assert.Equal("ACME Inc", result.Organization.Name);
        Assert.Equal("Active", result.Organization.Status);
        Assert.Equal("Pro", result.Organization.Plan);
        Assert.Equal(owner.Id, result.Organization.OwnerId);
        Assert.Equal("owner@example.com", result.OwnerInvite!.Email);
        Assert.Equal(OrganizationRole.Owner, result.OwnerInvite.Role);
        Assert.False(string.IsNullOrWhiteSpace(result.OwnerInvite.Token));

        repo.Verify(x => x.ExecuteOnboardingTransactionAsync("acme", It.IsAny<Func<Task>>()), Times.Once);
        repo.Verify(x => x.AddMemberAsync(It.Is<OrganizationMember>(m =>
            m.Role == OrganizationRole.Owner && m.Status == OrganizationMemberStatus.Invited)), Times.Once);
        repo.Verify(x => x.AddSystemRolesAsync(It.Is<IReadOnlyList<Role>>(roles =>
            roles.Count == 5 && roles.All(role => role.IsSystem))), Times.Once);
        repo.Verify(x => x.AddWorkspaceAsync(It.Is<Workspace>(w =>
            w.Slug == "acme-default" && w.OrganizationId == result.Organization.Id)), Times.Once);
        repo.Verify(x => x.AddLifecycleEventAsync(It.Is<OrganizationLifecycleEvent>(e => e.Kind == "Onboarded")), Times.Once);
        repo.Verify(x => x.SaveAsync(), Times.Once);
        email.Verify(e => e.SendAsync(
            It.Is<EmailMessage>(m => m.To == "owner@example.com"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_NonSuperAdmin_Forbidden_NothingSeeded()
    {
        var repo = Repo();
        var service = Service(repo, Tenant(isSuperAdmin: false));

        var error = await Assert.ThrowsAsync<DomainError>(() => service.CreateAsync(Guid.NewGuid(), Request()));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
        repo.Verify(x => x.AddOrganizationAsync(It.IsAny<Organization>()), Times.Never);
    }

    [Fact]
    public async Task Create_OwnerAccountMissing_Validation()
    {
        var repo = Repo(r => r.Setup(x => x.FindUserByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null));
        var service = Service(repo, Tenant());

        var error = await Assert.ThrowsAsync<DomainError>(() => service.CreateAsync(SuperAdminId, Request()));
        Assert.Equal(DomainErrorKind.Validation, error.Kind);
        repo.Verify(x => x.AddOrganizationAsync(It.IsAny<Organization>()), Times.Never);
    }

    [Theory]
    [InlineData("Bad Slug")]
    [InlineData("-leading-hyphen")]
    [InlineData("x")]
    [InlineData("")]
    public async Task Create_InvalidSlug_Validation(string slug)
    {
        var service = Service(Repo(), Tenant());
        var error = await Assert.ThrowsAsync<DomainError>(() => service.CreateAsync(SuperAdminId, Request(slug: slug)));
        Assert.Equal(DomainErrorKind.Validation, error.Kind);
    }

    [Fact]
    public async Task Create_EmailDeliveryFails_StateStillSeeded_BestEffort()
    {
        var owner = new User { Id = Guid.NewGuid(), Email = "owner@example.com" };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindUserByEmailAsync("owner@example.com")).ReturnsAsync(owner);
            r.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);
        });
        var email = Email(delivered: false);
        var service = Service(repo, Tenant(), email);

        var result = await service.CreateAsync(SuperAdminId, Request());

        Assert.NotNull(result.OwnerInvite);
        repo.Verify(x => x.AddOrganizationAsync(It.IsAny<Organization>()), Times.Once);
    }

    [Fact]
    public async Task List_NonSuperAdmin_Forbidden()
    {
        var service = Service(Repo(), Tenant(isSuperAdmin: false));
        var error = await Assert.ThrowsAsync<DomainError>(() => service.ListAsync(Guid.NewGuid(), 1, 50, null));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }

    // ── Suspend / Reactivate ──

    [Fact]
    public async Task Suspend_Happy_FlipsStatus_RecordsLifecycleAndAudit()
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "ACME", Slug = "acme", OwnerId = Guid.NewGuid() };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org);
            r.Setup(x => x.UpdateOrganizationAsync(It.IsAny<Organization>())).Returns(Task.CompletedTask);
            r.Setup(x => x.AddLifecycleEventAsync(It.IsAny<OrganizationLifecycleEvent>())).Returns(Task.CompletedTask);
            r.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);
        });
        var service = Service(repo, Tenant());

        await service.SuspendAsync(SuperAdminId, org.Id, new SuspendRequest("chargeback hold"));

        Assert.Equal(OrganizationStatus.Suspended, org.Status);
        repo.Verify(x => x.AddLifecycleEventAsync(It.Is<OrganizationLifecycleEvent>(e =>
            e.Kind == "Suspended" && e.PayloadJson!.Contains("chargeback hold"))), Times.Once);
        repo.Verify(x => x.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task Suspend_AlreadySuspended_Conflict()
    {
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = Guid.NewGuid(), Status = OrganizationStatus.Suspended };
        var repo = Repo(r => r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org));
        var service = Service(repo, Tenant());

        var error = await Assert.ThrowsAsync<DomainError>(() => service.SuspendAsync(SuperAdminId, org.Id, null));
        Assert.Equal(DomainErrorKind.Conflict, error.Kind);
    }

    [Fact]
    public async Task Suspend_NonSuperAdmin_Forbidden()
    {
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = Guid.NewGuid() };
        var repo = Repo(r => r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org));
        var service = Service(repo, Tenant(isSuperAdmin: false));

        var error = await Assert.ThrowsAsync<DomainError>(() => service.SuspendAsync(Guid.NewGuid(), org.Id, null));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }

    [Fact]
    public async Task Reactivate_Happy_RestoresActive()
    {
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = Guid.NewGuid(), Status = OrganizationStatus.Suspended };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org);
            r.Setup(x => x.UpdateOrganizationAsync(It.IsAny<Organization>())).Returns(Task.CompletedTask);
            r.Setup(x => x.AddLifecycleEventAsync(It.IsAny<OrganizationLifecycleEvent>())).Returns(Task.CompletedTask);
            r.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);
        });
        var service = Service(repo, Tenant());

        await service.ReactivateAsync(SuperAdminId, org.Id, null);

        Assert.Equal(OrganizationStatus.Active, org.Status);
        repo.Verify(x => x.AddLifecycleEventAsync(It.Is<OrganizationLifecycleEvent>(e => e.Kind == "Reactivated")), Times.Once);
    }

    [Fact]
    public async Task Reactivate_FromActive_Conflict()
    {
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = Guid.NewGuid() };
        var repo = Repo(r => r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org));
        var service = Service(repo, Tenant());

        var error = await Assert.ThrowsAsync<DomainError>(() => service.ReactivateAsync(SuperAdminId, org.Id, null));
        Assert.Equal(DomainErrorKind.Conflict, error.Kind);
    }

    // ── Transfer ownership ──

    [Fact]
    public async Task Transfer_Happy_PromotesNewOwner_DemotesPreviousOwner()
    {
        var previousOwnerId = Guid.NewGuid();
        var newOwnerUserId = Guid.NewGuid();
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = previousOwnerId };
        var previousOwnerMember = new OrganizationMember { OrganizationId = org.Id, UserId = previousOwnerId, Role = OrganizationRole.Owner, Status = OrganizationMemberStatus.Active };
        var newOwnerMember = new OrganizationMember { OrganizationId = org.Id, UserId = newOwnerUserId, Role = OrganizationRole.Member, Status = OrganizationMemberStatus.Active };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org);
            r.Setup(x => x.FindMemberAsync(org.Id, newOwnerUserId)).ReturnsAsync(newOwnerMember);
            r.Setup(x => x.FindMemberAsync(org.Id, previousOwnerId)).ReturnsAsync(previousOwnerMember);
            r.Setup(x => x.UpdateOrganizationAsync(It.IsAny<Organization>())).Returns(Task.CompletedTask);
            r.Setup(x => x.UpdateMemberAsync(It.IsAny<OrganizationMember>())).Returns(Task.CompletedTask);
            r.Setup(x => x.AddLifecycleEventAsync(It.IsAny<OrganizationLifecycleEvent>())).Returns(Task.CompletedTask);
            r.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);
        });
        var service = Service(repo, Tenant());

        await service.TransferOwnershipAsync(SuperAdminId, org.Id, new TransferOwnershipRequest(newOwnerUserId));

        Assert.Equal(newOwnerUserId, org.OwnerId);
        Assert.Equal(OrganizationRole.Owner, newOwnerMember.Role);
        Assert.Equal(OrganizationRole.Admin, previousOwnerMember.Role);
        repo.Verify(x => x.AddLifecycleEventAsync(It.Is<OrganizationLifecycleEvent>(e => e.Kind == "OwnershipTransferred")), Times.Once);
    }

    [Fact]
    public async Task Transfer_ToCurrentOwner_Conflict()
    {
        var ownerId = Guid.NewGuid();
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = ownerId };
        var repo = Repo(r => r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org));
        var service = Service(repo, Tenant());

        var error = await Assert.ThrowsAsync<DomainError>(
            () => service.TransferOwnershipAsync(SuperAdminId, org.Id, new TransferOwnershipRequest(ownerId)));
        Assert.Equal(DomainErrorKind.Conflict, error.Kind);
    }

    [Fact]
    public async Task Transfer_NonMember_NotFound()
    {
        var ownerId = Guid.NewGuid();
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = ownerId };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org);
            r.Setup(x => x.FindMemberAsync(org.Id, It.IsAny<Guid>())).ReturnsAsync((OrganizationMember?)null);
        });
        var service = Service(repo, Tenant());

        var error = await Assert.ThrowsAsync<DomainError>(
            () => service.TransferOwnershipAsync(SuperAdminId, org.Id, new TransferOwnershipRequest(Guid.NewGuid())));
        Assert.Equal(DomainErrorKind.NotFound, error.Kind);
    }

    [Fact]
    public async Task Transfer_InactiveMembership_Validation()
    {
        var ownerId = Guid.NewGuid();
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = ownerId };
        var invited = new OrganizationMember { OrganizationId = org.Id, UserId = Guid.NewGuid(), Role = OrganizationRole.Member, Status = OrganizationMemberStatus.Invited };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org);
            r.Setup(x => x.FindMemberAsync(org.Id, invited.UserId)).ReturnsAsync(invited);
        });
        var service = Service(repo, Tenant());

        var error = await Assert.ThrowsAsync<DomainError>(
            () => service.TransferOwnershipAsync(SuperAdminId, org.Id, new TransferOwnershipRequest(invited.UserId)));
        Assert.Equal(DomainErrorKind.Validation, error.Kind);
    }

    // ── Plan metadata ──

    [Fact]
    public async Task UpdatePlan_Happy_RecordsLifecycle_ReturnsDto()
    {
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = Guid.NewGuid(), PlanName = OrganizationPlan.Free };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org);
            r.Setup(x => x.UpdateOrganizationAsync(It.IsAny<Organization>())).Returns(Task.CompletedTask);
            r.Setup(x => x.AddLifecycleEventAsync(It.IsAny<OrganizationLifecycleEvent>())).Returns(Task.CompletedTask);
            r.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);
        });
        var service = Service(repo, Tenant());

        var dto = await service.UpdatePlanAsync(SuperAdminId, org.Id, new UpdatePlanRequest(OrganizationPlan.Enterprise));

        Assert.Equal("Enterprise", dto.Plan);
        Assert.Equal(OrganizationPlan.Enterprise, org.PlanName);
        repo.Verify(x => x.AddLifecycleEventAsync(It.Is<OrganizationLifecycleEvent>(e => e.Kind == "PlanChanged")), Times.Once);
    }

    [Fact]
    public async Task UpdatePlan_UndefinedPlanValue_Validation_NoChange()
    {
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = Guid.NewGuid(), PlanName = OrganizationPlan.Free };
        var repo = Repo(r => r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org));
        var service = Service(repo, Tenant());

        var error = await Assert.ThrowsAsync<DomainError>(
            () => service.UpdatePlanAsync(SuperAdminId, org.Id, new UpdatePlanRequest((OrganizationPlan)999)));

        Assert.Equal(DomainErrorKind.Validation, error.Kind);
        Assert.Equal(OrganizationPlan.Free, org.PlanName);
        repo.Verify(x => x.UpdateOrganizationAsync(It.IsAny<Organization>()), Times.Never);
        repo.Verify(x => x.AddLifecycleEventAsync(It.IsAny<OrganizationLifecycleEvent>()), Times.Never);
    }

    [Fact]
    public async Task Create_UndefinedPlanValue_Validation_NothingSeeded()
    {
        var owner = new User { Id = Guid.NewGuid(), Email = "owner@example.com" };
        var repo = Repo(r => r.Setup(x => x.FindUserByEmailAsync("owner@example.com")).ReturnsAsync(owner));
        var service = Service(repo, Tenant());

        var error = await Assert.ThrowsAsync<DomainError>(() => service.CreateAsync(
            SuperAdminId, new CreateOrganizationRequest("ACME Inc", "acme", "owner@example.com", (OrganizationPlan)999)));

        Assert.Equal(DomainErrorKind.Validation, error.Kind);
        repo.Verify(x => x.AddOrganizationAsync(It.IsAny<Organization>()), Times.Never);
        repo.Verify(x => x.AddMemberAsync(It.IsAny<OrganizationMember>()), Times.Never);
        repo.Verify(x => x.AddInviteAsync(It.IsAny<OrganizationInvite>()), Times.Never);
    }

    // ── Transition audit (status + reason in After) ──

    [Fact]
    public async Task Suspend_And_Reactivate_QueueAuditWithStatusAndReason()
    {
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = Guid.NewGuid(), Status = OrganizationStatus.Active };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org);
            r.Setup(x => x.UpdateOrganizationAsync(It.IsAny<Organization>())).Returns(Task.CompletedTask);
            r.Setup(x => x.AddLifecycleEventAsync(It.IsAny<OrganizationLifecycleEvent>())).Returns(Task.CompletedTask);
            r.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);
        });
        var audit = new Mock<IAuditService>();
        var service = Service(repo, Tenant(), audit: audit);

        await service.SuspendAsync(SuperAdminId, org.Id, new SuspendRequest("chargeback"));
        await service.ReactivateAsync(SuperAdminId, org.Id, null);

        audit.Verify(x => x.QueueAudit(It.Is<AuditEntry>(e =>
            e.Action == "Organization.Suspended"
            && e.After!.Contains("Suspended")
            && e.After!.Contains("chargeback"))), Times.Once);
        audit.Verify(x => x.QueueAudit(It.Is<AuditEntry>(e =>
            e.Action == "Organization.Reactivated"
            && e.After!.Contains("Active"))), Times.Once);
    }

    // ── Invite accept ──

    [Fact]
    public async Task Accept_Happy_ActivatesOwnerMembership_ConsumesInvite()
    {
        var owner = new User { Id = Guid.NewGuid(), Email = "owner@example.com" };
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = owner.Id };
        var invite = new OrganizationInvite { Id = Guid.NewGuid(), OrganizationId = org.Id, Email = "owner@example.com", Role = OrganizationRole.Owner, Token = "tok", Status = OrganizationMemberStatus.Invited, ExpiresAt = DateTime.UtcNow.AddDays(7) };
        var invitedMember = new OrganizationMember { OrganizationId = org.Id, UserId = owner.Id, Role = OrganizationRole.Owner, Status = OrganizationMemberStatus.Invited };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindActiveInviteByTokenAsync("tok")).ReturnsAsync(invite);
            r.Setup(x => x.FindUserByIdAsync(owner.Id)).ReturnsAsync(owner);
            r.Setup(x => x.FindMemberAsync(org.Id, owner.Id)).ReturnsAsync(invitedMember);
            r.Setup(x => x.UpdateMemberAsync(It.IsAny<OrganizationMember>())).Returns(Task.CompletedTask);
            r.Setup(x => x.UpdateInviteAsync(It.IsAny<OrganizationInvite>())).Returns(Task.CompletedTask);
            r.Setup(x => x.SaveAsync()).Returns(Task.CompletedTask);
        });
        var service = Service(repo, Tenant());

        await service.AcceptInviteAsync("tok", owner.Id);

        Assert.Equal(OrganizationMemberStatus.Active, invitedMember.Status);
        Assert.Equal(OrganizationMemberStatus.Active, invite.Status);
        Assert.NotNull(invite.AcceptedAt);
    }

    [Fact]
    public async Task Accept_EmailMismatch_Forbidden()
    {
        var owner = new User { Id = Guid.NewGuid(), Email = "owner@example.com" };
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = owner.Id };
        var invite = new OrganizationInvite { Id = Guid.NewGuid(), OrganizationId = org.Id, Email = "other@example.com", Role = OrganizationRole.Owner, Token = "tok", Status = OrganizationMemberStatus.Invited, ExpiresAt = DateTime.UtcNow.AddDays(7) };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindActiveInviteByTokenAsync("tok")).ReturnsAsync(invite);
            r.Setup(x => x.FindUserByIdAsync(owner.Id)).ReturnsAsync(owner);
        });
        var service = Service(repo, Tenant());

        var error = await Assert.ThrowsAsync<DomainError>(() => service.AcceptInviteAsync("tok", owner.Id));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }

    [Fact]
    public async Task Accept_UnknownToken_NotFound()
    {
        var repo = Repo(r => r.Setup(x => x.FindActiveInviteByTokenAsync(It.IsAny<string>())).ReturnsAsync((OrganizationInvite?)null));
        var service = Service(repo, Tenant());

        var error = await Assert.ThrowsAsync<DomainError>(() => service.AcceptInviteAsync("tok", Guid.NewGuid()));
        Assert.Equal(DomainErrorKind.NotFound, error.Kind);
    }

    [Fact]
    public async Task Accept_Unauthenticated_Forbidden()
    {
        var service = Service(Repo(), Tenant());
        var error = await Assert.ThrowsAsync<DomainError>(() => service.AcceptInviteAsync("tok", Guid.Empty));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }

    [Fact]
    public async Task GetById_CompanyAdminOfOwnCompany_Allowed()
    {
        var ownerId = Guid.NewGuid();
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = ownerId };
        var adminMember = new OrganizationMember { OrganizationId = org.Id, UserId = ownerId, Role = OrganizationRole.Owner, Status = OrganizationMemberStatus.Active };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org);
            r.Setup(x => x.FindMemberAsync(org.Id, ownerId)).ReturnsAsync(adminMember);
        });
        var service = Service(repo, Tenant(isSuperAdmin: false));

        var dto = await service.GetByIdAsync(ownerId, org.Id);
        Assert.Equal(org.Id, dto.Id);
    }

    [Fact]
    public async Task GetById_MemberOfOtherCompany_Forbidden()
    {
        var org = new Organization { Id = Guid.NewGuid(), Slug = "acme", OwnerId = Guid.NewGuid() };
        // An Active Owner of a DIFFERENT company: not a member of org at all, so
        // FindMemberAsync(org.Id, user) returns null and the view is Forbidden.
        var otherOrganizationId = Guid.NewGuid();
        var memberOfOther = new OrganizationMember
        {
            OrganizationId = otherOrganizationId, UserId = Guid.NewGuid(),
            Role = OrganizationRole.Owner, Status = OrganizationMemberStatus.Active
        };
        var repo = Repo(r =>
        {
            r.Setup(x => x.FindOrganizationByIdAsync(org.Id)).ReturnsAsync(org);
            r.Setup(x => x.FindMemberAsync(org.Id, memberOfOther.UserId)).ReturnsAsync((OrganizationMember?)null);
        });
        var service = Service(repo, Tenant(isSuperAdmin: false));

        var error = await Assert.ThrowsAsync<DomainError>(() => service.GetByIdAsync(memberOfOther.UserId, org.Id));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }
}
