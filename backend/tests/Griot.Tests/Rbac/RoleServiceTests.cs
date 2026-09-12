using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Griot.Application.Authorization;
using Griot.Application.DTOs.Roles;
using Griot.Application.Interfaces.Repositories;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Griot.Tests.Rbac;

/// <summary>
/// Spec 31 unit tests for <see cref="RoleService"/>: system-role seeding, custom-role
/// CRUD (fixed catalogue + no-escalation), member role assignment, force-revoke and
/// the custom-role delete cascade (members -> system Member, refresh families revoked).
/// </summary>
public class RoleServiceTests
{
    private static readonly Guid Org = Guid.NewGuid();
    private static readonly Guid Caller = Guid.NewGuid();

    private static OrganizationMember Member(Guid userId, OrganizationRole role, Guid? customRoleId = null) =>
        new() { Id = Guid.NewGuid(), OrganizationId = Org, UserId = userId, Role = role, CustomRoleId = customRoleId, Status = OrganizationMemberStatus.Active };

    private static Role CustomRole(string name = "QA Lead", string perms = PermissionCatalogue.TaskManage) =>
        new() { Id = Guid.NewGuid(), OrganizationId = Org, Name = name, IsSystem = false, Permissions = perms };

    private static Role SystemRole(string name) =>
        new() { Id = Guid.NewGuid(), OrganizationId = Org, Name = name, IsSystem = true, Permissions = string.Empty };

    private static Mock<IPermissionService> PermMock(IReadOnlySet<string>? perms = null, string? effectiveRole = null)
    {
        var mock = new Mock<IPermissionService>();
        mock.Setup(p => p.HasPermissionAsync(It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        mock.Setup(p => p.EffectivePermissionsAsync(It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync(perms ?? new HashSet<string>(PermissionCatalogue.All, StringComparer.Ordinal));
        mock.Setup(p => p.GetEffectiveRoleAsync(It.IsAny<Guid>(), It.IsAny<Guid?>()))
            .ReturnsAsync(effectiveRole ?? RoleSelection.RoleOwner);
        return mock;
    }

    private static (RoleService Svc, Mock<IRoleRepository> Roles, Mock<IAuthRepository> Auth, Mock<IAuditService> Audit) Build(
        Mock<IPermissionService>? perms = null)
    {
        var roles = new Mock<IRoleRepository>();
        var auth = new Mock<IAuthRepository>();
        var audit = new Mock<IAuditService>();
        audit.Setup(a => a.RecordAsync(It.IsAny<AuditEntry>())).Returns(Task.CompletedTask);
        var svc = new RoleService(roles.Object, auth.Object, (perms ?? PermMock()).Object, audit.Object, NullLogger<RoleService>.Instance);
        return (svc, roles, auth, audit);
    }

    // -- System-role seeding --

    [Fact]
    public async Task EnsureSystemRoles_SeedsFiveRoles_ThenIdempotent()
    {
        var (svc, roles, _, _) = Build();
        roles.Setup(r => r.CountSystemRolesAsync(Org)).ReturnsAsync(0);
        roles.Setup(r => r.FindByNameAsync(Org, It.IsAny<string>())).ReturnsAsync((Role?)null);

        var created = await svc.EnsureSystemRolesAsync(Org);
        Assert.Equal(5, created);
        roles.Verify(r => r.AddAsync(It.Is<Role>(x => x.IsSystem && x.OrganizationId == Org)), Times.Exactly(5));

        roles.Setup(r => r.CountSystemRolesAsync(Org)).ReturnsAsync(5);
        Assert.Equal(0, await svc.EnsureSystemRolesAsync(Org));
        roles.Verify(r => r.AddAsync(It.IsAny<Role>()), Times.Exactly(5));
    }

    [Fact]
    public async Task BackfillSystemRoles_SkipsOrganizationsAlreadySeeded()
    {
        var (svc, roles, _, _) = Build();
        var seededOrg = Guid.NewGuid();
        roles.Setup(r => r.ListActiveOrganizationIdsMissingSystemRolesAsync())
            .ReturnsAsync(new List<Guid> { Org, seededOrg });
        roles.Setup(r => r.CountSystemRolesAsync(Org)).ReturnsAsync(5);
        roles.Setup(r => r.CountSystemRolesAsync(seededOrg)).ReturnsAsync(0);
        roles.Setup(r => r.FindByNameAsync(seededOrg, It.IsAny<string>())).ReturnsAsync((Role?)null);

        var created = await svc.BackfillSystemRolesAsync();
        Assert.Equal(5, created);
        roles.Verify(r => r.AddAsync(It.Is<Role>(x => x.OrganizationId == Org)), Times.Never);
        roles.Verify(r => r.AddAsync(It.Is<Role>(x => x.OrganizationId == seededOrg)), Times.Exactly(5));
    }

    // -- Custom-role CRUD --

    [Fact]
    public async Task Create_UnknownPermissionKey_ThrowsValidation()
    {
        var (svc, _, _, _) = Build();
        var error = await Assert.ThrowsAsync<DomainError>(() =>
            svc.CreateAsync(Caller, Org, new CreateRoleRequest("QA", new[] { "not.a.key" })));
        Assert.Equal(DomainErrorKind.Validation, error.Kind);
    }

    [Fact]
    public async Task Create_DuplicateName_ThrowsConflict()
    {
        var (svc, roles, _, _) = Build();
        roles.Setup(r => r.FindByNameAsync(Org, "QA")).ReturnsAsync(CustomRole());
        var error = await Assert.ThrowsAsync<DomainError>(() =>
            svc.CreateAsync(Caller, Org, new CreateRoleRequest("QA", new[] { PermissionCatalogue.TaskManage })));
        Assert.Equal(DomainErrorKind.Conflict, error.Kind);
    }

    [Fact]
    public async Task Create_GrantingPermissionCallerDoesNotHold_ThrowsForbidden()
    {
        var callerPerms = new HashSet<string>(StringComparer.Ordinal) { PermissionCatalogue.OrgRead, PermissionCatalogue.OrgRolesManage };
        var (svc, _, _, _) = Build(PermMock(callerPerms));
        var error = await Assert.ThrowsAsync<DomainError>(() =>
            svc.CreateAsync(Caller, Org, new CreateRoleRequest("QA", new[] { PermissionCatalogue.TaskManage })));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }

    [Fact]
    public async Task Create_ValidCustomRole_PersistsWithOrderedPerms()
    {
        var (svc, roles, _, audit) = Build();
        roles.Setup(r => r.FindByNameAsync(Org, "QA")).ReturnsAsync((Role?)null);
        Role? added = null;
        roles.Setup(r => r.AddAsync(It.IsAny<Role>()))
            .Callback<Role>(r => added = r)
            .ReturnsAsync((Role r) => r);

        var dto = await svc.CreateAsync(Caller, Org, new CreateRoleRequest(
            "QA", new[] { PermissionCatalogue.TaskManage, PermissionCatalogue.CommentWrite }));

        Assert.False(dto.IsSystem);
        Assert.Equal(new[] { PermissionCatalogue.CommentWrite, PermissionCatalogue.TaskManage }, dto.Permissions);
        Assert.Equal($"{PermissionCatalogue.CommentWrite}, {PermissionCatalogue.TaskManage}", added!.Permissions);
        audit.Verify(a => a.RecordAsync(It.Is<AuditEntry>(e => e.Action == "Role.Created")), Times.Once);
    }

    [Fact]
    public async Task Update_SystemRole_ThrowsForbidden()
    {
        var (svc, roles, _, _) = Build();
        roles.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(SystemRole("Admin"));
        var error = await Assert.ThrowsAsync<DomainError>(() =>
            svc.UpdateAsync(Caller, Org, Guid.NewGuid(), new UpdateRoleRequest("Renamed", null)));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }

    [Fact]
    public async Task Update_CrossTenantRole_ThrowsForbidden()
    {
        var (svc, roles, _, _) = Build();
        var foreign = CustomRole(); foreign.OrganizationId = Guid.NewGuid();
        roles.Setup(r => r.FindByIdAsync(foreign.Id)).ReturnsAsync(foreign);
        var error = await Assert.ThrowsAsync<DomainError>(() =>
            svc.UpdateAsync(Caller, Org, foreign.Id, new UpdateRoleRequest(null, new List<string>())));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }

    // -- Custom-role delete cascade (approved behavior: members → system Member) --

    [Fact]
    public async Task Delete_SystemRole_ThrowsForbidden()
    {
        var (svc, roles, _, _) = Build();
        roles.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(SystemRole("Owner"));
        var error = await Assert.ThrowsAsync<DomainError>(() =>
            svc.DeleteAsync(Caller, Org, Guid.NewGuid()));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }

    [Fact]
    public async Task Delete_CrossTenantRole_ThrowsForbidden()
    {
        var (svc, roles, _, _) = Build();
        var foreign = CustomRole(); foreign.OrganizationId = Guid.NewGuid();
        roles.Setup(r => r.FindByIdAsync(foreign.Id)).ReturnsAsync(foreign);
        var error = await Assert.ThrowsAsync<DomainError>(() =>
            svc.DeleteAsync(Caller, Org, foreign.Id));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }

    [Fact]
    public async Task Delete_CustomRole_DemotesMembersToSystemMember_AndRevokesFamilies()
    {
        var (svc, roles, auth, audit) = Build();
        var role = CustomRole();
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        roles.Setup(r => r.FindByIdAsync(role.Id)).ReturnsAsync(role);
        roles.Setup(r => r.DemoteCustomRoleMembersAsync(Org, role.Id))
            .ReturnsAsync(new List<Guid> { u1, u2, u1 }); // duplicate user id must collapse

        await svc.DeleteAsync(Caller, Org, role.Id);

        roles.Verify(r => r.DeleteAsync(role), Times.Once);
        auth.Verify(a => a.RevokeAllFamiliesAsync(u1, It.IsAny<DateTime>()), Times.Once);
        auth.Verify(a => a.RevokeAllFamiliesAsync(u2, It.IsAny<DateTime>()), Times.Once);
        audit.Verify(a => a.RecordAsync(It.Is<AuditEntry>(e => e.Action == "Role.Deleted"
            && e.After!.Contains("\"AffectedUsers\":2") && e.After.Contains("\"FamiliesRevoked\":2"))), Times.Once);
    }

    [Fact]
    public async Task Delete_CustomRole_NoMembers_StillDeletes()
    {
        var (svc, roles, auth, _) = Build();
        var role = CustomRole();
        roles.Setup(r => r.FindByIdAsync(role.Id)).ReturnsAsync(role);
        roles.Setup(r => r.DemoteCustomRoleMembersAsync(Org, role.Id))
            .ReturnsAsync(new List<Guid>());

        await svc.DeleteAsync(Caller, Org, role.Id);

        roles.Verify(r => r.DeleteAsync(role), Times.Once);
        auth.Verify(a => a.RevokeAllFamiliesAsync(It.IsAny<Guid>(), It.IsAny<DateTime>()), Times.Never);
    }

    // -- Force-revoke --

    [Fact]
    public async Task ForceRevoke_SystemRole_RevokesAllHolders()
    {
        var (svc, roles, auth, _) = Build();
        var u1 = Guid.NewGuid();
        var u2 = Guid.NewGuid();
        roles.Setup(r => r.FindByIdAsync(It.IsAny<Guid>())).ReturnsAsync(SystemRole("ProjectManager"));
        roles.Setup(r => r.ListActiveMemberUserIdsForSystemRoleAsync(Org, OrganizationRole.ProjectManager))
            .ReturnsAsync(new List<Guid> { u1, u2 });

        var result = await svc.ForceRevokeAsync(Caller, Org, Guid.NewGuid());

        Assert.Equal(2, result.AffectedUsers);
        Assert.Equal(2, result.FamiliesRevoked);
        auth.Verify(a => a.RevokeAllFamiliesAsync(u1, It.IsAny<DateTime>()), Times.Once);
        auth.Verify(a => a.RevokeAllFamiliesAsync(u2, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task ForceRevoke_CustomRole_RevokesAllHolders()
    {
        var (svc, roles, auth, _) = Build();
        var role = CustomRole();
        var u1 = Guid.NewGuid();
        roles.Setup(r => r.FindByIdAsync(role.Id)).ReturnsAsync(role);
        roles.Setup(r => r.ListActiveMembersForCustomRoleAsync(role.Id))
            .ReturnsAsync(new List<OrganizationMember> { Member(u1, OrganizationRole.Custom, role.Id) });

        var result = await svc.ForceRevokeAsync(Caller, Org, role.Id);

        Assert.Equal(1, result.AffectedUsers);
        Assert.Equal(1, result.FamiliesRevoked);
        auth.Verify(a => a.RevokeAllFamiliesAsync(u1, It.IsAny<DateTime>()), Times.Once);
    }

    // -- Member role assignment --

    [Fact]
    public async Task SetMemberRole_SelfChange_ThrowsForbidden()
    {
        var (svc, roles, _, _) = Build();
        var member = Member(Caller, OrganizationRole.Member);
        roles.Setup(r => r.FindMemberByIdAsync(Org, member.Id)).ReturnsAsync(member);

        var error = await Assert.ThrowsAsync<DomainError>(() =>
            svc.SetMemberRoleAsync(Caller, Org, member.Id, new SetMemberRoleRequest("Admin")));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }

    [Fact]
    public async Task SetMemberRole_UnknownRoleName_ThrowsValidation()
    {
        var (svc, roles, _, _) = Build();
        var member = Member(Guid.NewGuid(), OrganizationRole.Member);
        roles.Setup(r => r.FindMemberByIdAsync(Org, member.Id)).ReturnsAsync(member);

        var error = await Assert.ThrowsAsync<DomainError>(() =>
            svc.SetMemberRoleAsync(Caller, Org, member.Id, new SetMemberRoleRequest("Ninja")));
        Assert.Equal(DomainErrorKind.Validation, error.Kind);
    }

    [Fact]
    public async Task SetMemberRole_SystemRole_UpdatesAndAudits()
    {
        var (svc, roles, _, audit) = Build();
        var member = Member(Guid.NewGuid(), OrganizationRole.Member);
        roles.Setup(r => r.FindMemberByIdAsync(Org, member.Id)).ReturnsAsync(member);

        var result = await svc.SetMemberRoleAsync(Caller, Org, member.Id, new SetMemberRoleRequest("projectmanager"));

        Assert.Equal(RoleSelection.RoleProjectManager, result.Role);
        Assert.Null(result.CustomRoleId);
        roles.Verify(r => r.UpdateMemberRoleAsync(It.Is<OrganizationMember>(
            m => m.Role == OrganizationRole.ProjectManager && m.CustomRoleId == null)), Times.Once);
        audit.Verify(a => a.RecordAsync(It.Is<AuditEntry>(e => e.Action == "Member.RoleChanged")), Times.Once);
    }

    [Fact]
    public async Task SetMemberRole_CustomRole_SetsCustomRoleId()
    {
        var (svc, roles, _, _) = Build();
        var role = CustomRole("QA Lead");
        var member = Member(Guid.NewGuid(), OrganizationRole.Member);
        roles.Setup(r => r.FindMemberByIdAsync(Org, member.Id)).ReturnsAsync(member);
        roles.Setup(r => r.FindByIdAsync(role.Id)).ReturnsAsync(role);

        var result = await svc.SetMemberRoleAsync(Caller, Org, member.Id,
            new SetMemberRoleRequest($"{RoleSelection.RoleCustomPrefix}{role.Id}"));

        Assert.Equal($"{RoleSelection.RoleCustomPrefix}{role.Id}", result.Role);
        Assert.Equal(role.Id, result.CustomRoleId);
        roles.Verify(r => r.UpdateMemberRoleAsync(It.Is<OrganizationMember>(
            m => m.Role == OrganizationRole.Custom && m.CustomRoleId == role.Id)), Times.Once);
    }

    [Fact]
    public async Task SetMemberRole_ForeignCustomRole_ThrowsValidation()
    {
        var (svc, roles, _, _) = Build();
        var foreignRole = CustomRole(); foreignRole.OrganizationId = Guid.NewGuid();
        var member = Member(Guid.NewGuid(), OrganizationRole.Member);
        roles.Setup(r => r.FindMemberByIdAsync(Org, member.Id)).ReturnsAsync(member);
        roles.Setup(r => r.FindByIdAsync(foreignRole.Id)).ReturnsAsync(foreignRole);

        var error = await Assert.ThrowsAsync<DomainError>(() =>
            svc.SetMemberRoleAsync(Caller, Org, member.Id,
                new SetMemberRoleRequest($"{RoleSelection.RoleCustomPrefix}{foreignRole.Id}")));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind); // cross-tenant custom role → 403
    }

    [Fact]
    public async Task SetMemberRole_CustomLiteralName_ThrowsValidation()
    {
        var (svc, roles, _, _) = Build();
        var member = Member(Guid.NewGuid(), OrganizationRole.Member);
        roles.Setup(r => r.FindMemberByIdAsync(Org, member.Id)).ReturnsAsync(member);

        var error = await Assert.ThrowsAsync<DomainError>(() =>
            svc.SetMemberRoleAsync(Caller, Org, member.Id, new SetMemberRoleRequest("Custom")));
        Assert.Equal(DomainErrorKind.Validation, error.Kind);
    }

    [Fact]
    public async Task SetMemberRole_DemoteOwner_NonOwnerCaller_ThrowsForbidden()
    {
        var nonOwnerPerms = new HashSet<string>(StringComparer.Ordinal)
        {
            PermissionCatalogue.OrgRead, PermissionCatalogue.OrgMembersManage
        };
        var (svc, roles, _, _) = Build(PermMock(nonOwnerPerms, RoleSelection.RoleAdmin));
        var owner = Member(Guid.NewGuid(), OrganizationRole.Owner);
        roles.Setup(r => r.FindMemberByIdAsync(Org, owner.Id)).ReturnsAsync(owner);

        var error = await Assert.ThrowsAsync<DomainError>(() =>
            svc.SetMemberRoleAsync(Caller, Org, owner.Id, new SetMemberRoleRequest("Member")));
        Assert.Equal(DomainErrorKind.Forbidden, error.Kind);
    }

    [Fact]
    public async Task SetMemberRole_DemoteOwner_OwnerCaller_Succeeds()
    {
        var (svc, roles, _, _) = Build();
        var owner = Member(Guid.NewGuid(), OrganizationRole.Owner);
        roles.Setup(r => r.FindMemberByIdAsync(Org, owner.Id)).ReturnsAsync(owner);

        var result = await svc.SetMemberRoleAsync(Caller, Org, owner.Id, new SetMemberRoleRequest("Member"));
        Assert.Equal(RoleSelection.RoleMember, result.Role);
    }

    // -- List --

    [Fact]
    public async Task List_ReturnsSystemAndCustomRoles()
    {
        var (svc, roles, _, _) = Build();
        roles.Setup(r => r.ListForOrganizationAsync(Org)).ReturnsAsync(new List<Role>
        {
            SystemRole("Owner"),
            CustomRole("QA Lead")
        });

        var result = await svc.ListAsync(Caller, Org);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.IsSystem && r.Name == "Owner");
        Assert.Contains(result, r => !r.IsSystem && r.Name == "QA Lead");
    }
}
