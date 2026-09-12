using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Griot.Application.Authorization;
using Griot.Application.DTOs.Roles;
using Griot.Application.Interfaces.Services;
using Griot.Application.Services;
using Griot.Application.Tenancy;
using Griot.Domain.Entities;
using Griot.Domain.Enums;
using Griot.Infrastructure.Persistence;
using Griot.Infrastructure.Repositories;
using Griot.Tests.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace Griot.Tests.Rbac;

public sealed class RoleSqlTests : IClassFixture<SqlAuthFixture>
{
    private readonly SqlAuthFixture _fixture;
    public RoleSqlTests(SqlAuthFixture fixture) => _fixture = fixture;

    private static User User() => new()
    {
        Email = $"rbac-{Guid.NewGuid():N}@example.com", DisplayName = "RBAC test", PasswordHash = "test"
    };

    private async Task<(Organization Org, User Owner)> SeedAsync()
    {
        await using var db = _fixture.CreateContext();
        var owner = User();
        var org = new Organization { Name = "RBAC test", Slug = Guid.NewGuid().ToString("N"), OwnerId = owner.Id };
        db.Users.Add(owner);
        db.Organizations.Add(org);
        db.OrganizationMembers.Add(new OrganizationMember
        {
            OrganizationId = org.Id, UserId = owner.Id, Role = OrganizationRole.Owner,
            Status = OrganizationMemberStatus.Active
        });
        await db.SaveChangesAsync();
        return (org, owner);
    }

    private static RoleService Service(GriotDbContext db, Guid? orgId, bool failAudit = false)
    {
        var tenant = new Mock<ITenantContext>();
        tenant.SetupGet(t => t.OrganizationId).Returns(orgId);
        tenant.SetupGet(t => t.IsOrganizationActive).Returns(true);
        db.WithTenant(tenant.Object);
        var auth = new AuthRepository(db);
        var audit = new Mock<IAuditService>();
        audit.Setup(a => a.RecordAsync(It.IsAny<AuditEntry>())).Returns<AuditEntry>(async entry =>
        {
            if (failAudit) throw new InvalidOperationException("audit unavailable");
            db.AuditLogs.Add(new AuditLog
            {
                ActorId = entry.ActorId, OrganizationId = entry.OrganizationId,
                EntityType = entry.EntityType, EntityId = entry.EntityId, Action = entry.Action,
                Before = entry.Before, After = entry.After
            });
            await db.SaveChangesAsync();
        });
        return new RoleService(new RoleRepository(db), auth, new PermissionService(auth), audit.Object,
            NullLogger<RoleService>.Instance, tenant.Object);
    }

    [SqlAuthFact]
    public async Task PartialStartupSeed_RepairsWithoutTenant_AndConcurrentSeedsStayUnique()
    {
        var (org, _) = await SeedAsync();
        await using (var setup = _fixture.CreateContext())
        {
            setup.Roles.Add(new Role { OrganizationId = org.Id, Name = "Owner", IsSystem = true });
            await setup.SaveChangesAsync();
        }
        await using var first = _fixture.CreateContext();
        await using var second = _fixture.CreateContext();
        Assert.Contains(org.Id, await new RoleRepository(first).ListActiveOrganizationIdsMissingSystemRolesAsync());
        var counts = await Task.WhenAll(Service(first, null).EnsureSystemRolesAsync(org.Id),
            Service(second, null).EnsureSystemRolesAsync(org.Id));
        Assert.Equal(4, counts.Sum());
        await using var verify = _fixture.CreateContext();
        var roles = await verify.Roles.IgnoreQueryFilters().Where(r => r.OrganizationId == org.Id).ToListAsync();
        Assert.Equal(5, roles.Count);
        Assert.Equal(5, roles.Select(r => r.Name).Distinct().Count());
    }

    [SqlAuthFact]
    public async Task ConcurrentCustomRoleCreation_OneSuccessOneConflict()
    {
        var (org, owner) = await SeedAsync();
        async Task<bool> Create()
        {
            await using var db = _fixture.CreateContext();
            try
            {
                await Service(db, org.Id).CreateAsync(owner.Id, org.Id,
                    new CreateRoleRequest("Reviewers", new[] { PermissionCatalogue.OrgRead }));
                return true;
            }
            catch (DomainError error) when (error.Kind == DomainErrorKind.Conflict) { return false; }
        }
        var results = await Task.WhenAll(Create(), Create());
        Assert.Single(results.Where(r => r));
    }

    [SqlAuthFact]
    public async Task DeleteRole_HandlesInactiveMembersAndInvites_AndAuditFailureRollsEverythingBack()
    {
        var (org, owner) = await SeedAsync();
        var holder = User();
        var role = new Role { OrganizationId = org.Id, Name = "Reviewers", Permissions = PermissionCatalogue.OrgRead };
        var token = new RefreshToken
        {
            UserId = holder.Id, TokenHash = Guid.NewGuid().ToString("N"), FamilyId = Guid.NewGuid(),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };
        await using (var setup = _fixture.CreateContext())
        {
            setup.Users.Add(holder);
            setup.Roles.Add(role);
            setup.OrganizationMembers.Add(new OrganizationMember
            {
                OrganizationId = org.Id, UserId = holder.Id, Role = OrganizationRole.Custom,
                CustomRoleId = role.Id, Status = OrganizationMemberStatus.Suspended
            });
            setup.OrganizationInvites.Add(new OrganizationInvite
            {
                OrganizationId = org.Id, Role = OrganizationRole.Custom, CustomRoleId = role.Id,
                Email = "invite@example.com", Token = Guid.NewGuid().ToString("N"), InvitedById = owner.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
            setup.RefreshTokens.Add(token);
            await setup.SaveChangesAsync();
        }
        await using (var failed = _fixture.CreateContext())
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Service(failed, org.Id, failAudit: true).DeleteAsync(owner.Id, org.Id, role.Id));
        await using (var verify = _fixture.CreateContext())
        {
            Assert.True(await verify.Roles.IgnoreQueryFilters().AnyAsync(r => r.Id == role.Id));
            Assert.Null((await verify.RefreshTokens.SingleAsync(t => t.Id == token.Id)).RevokedAt);
            Assert.Equal(role.Id, (await verify.OrganizationMembers.IgnoreQueryFilters()
                .SingleAsync(m => m.UserId == holder.Id)).CustomRoleId);
        }
        await using (var success = _fixture.CreateContext())
            await Service(success, org.Id).DeleteAsync(owner.Id, org.Id, role.Id);
        await using var final = _fixture.CreateContext();
        Assert.False(await final.Roles.IgnoreQueryFilters().AnyAsync(r => r.Id == role.Id));
        Assert.Null((await final.OrganizationMembers.IgnoreQueryFilters().SingleAsync(m => m.UserId == holder.Id)).CustomRoleId);
        Assert.Null((await final.OrganizationInvites.IgnoreQueryFilters().SingleAsync(i => i.OrganizationId == org.Id)).CustomRoleId);
        Assert.NotNull((await final.RefreshTokens.SingleAsync(t => t.Id == token.Id)).RevokedAt);
        Assert.True(await final.AuditLogs.IgnoreQueryFilters().AnyAsync(a => a.EntityId == role.Id && a.Action == "Role.Deleted"));
    }

    [SqlAuthFact]
    public async Task HttpAndGraphqlPolicies_UseDatabasePermissions_AndRejectCrossTenantWrites()
    {
        var (org, owner) = await SeedAsync();
        var (other, _) = await SeedAsync();
        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var rawRefresh = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        await using (var tokens = _fixture.CreateContext())
        {
            tokens.RefreshTokens.Add(new RefreshToken
            {
                UserId = owner.Id, FamilyId = Guid.NewGuid(), ExpiresAt = DateTime.UtcNow.AddDays(1),
                TokenHash = Convert.ToHexString(SHA256.HashData(Convert.FromHexString(rawRefresh)))
            });
            await tokens.SaveChangesAsync();
        }
        using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder
            .UseContentRoot(_fixture.ApiDirectory).UseEnvironment("Production")
            .ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _fixture.ConnectionString,
                ["JWT:Key"] = key, ["JWT:Issuer"] = "Griot", ["JWT:Audience"] = "GriotClients",
                ["Brevo:ApiKey"] = "", ["BREVO_API_KEY"] = "",
                ["SuperAdmin:Email"] = "", ["SUPERADMIN__EMAIL"] = ""
            }))
            .ConfigureTestServices(services =>
            {
                services.RemoveAll<GriotDbContext>();
                services.RemoveAll<IDbContextFactory<GriotDbContext>>();
                services.AddScoped(sp => _fixture.CreateContext().WithTenant(sp.GetRequiredService<ITenantContext>()));
                services.AddSingleton<IDbContextFactory<GriotDbContext>>(new ContextFactory(_fixture));
            }));
        using var client = app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using (var scope = app.Services.CreateScope())
            Assert.Equal(_fixture.ConnectionString, scope.ServiceProvider.GetRequiredService<GriotDbContext>().Database.GetConnectionString());
        var jwt = new JwtSecurityToken("Griot", "GriotClients", new[]
        {
            new Claim("sub", owner.Id.ToString()), new Claim("org", org.Id.ToString()),
            new Claim("role", "owner"), new Claim("perms", PermissionCatalogue.Join(PermissionCatalogue.All))
        }, expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        using var initial = await client.GetAsync($"/api/organizations/{org.Id}/roles");
        Assert.True(initial.StatusCode == HttpStatusCode.OK,
            $"{initial.StatusCode}: {await initial.Content.ReadAsStringAsync()}");
        var query = new { query = "{ organizationRoles { id name } }" };
        using (var result = await client.PostAsJsonAsync("/graphql", query))
        {
            var json = JsonDocument.Parse(await result.Content.ReadAsStringAsync());
            Assert.False(json.RootElement.TryGetProperty("errors", out _), json.RootElement.ToString());
            Assert.Equal(5, json.RootElement.GetProperty("data").GetProperty("organizationRoles").GetArrayLength());
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsJsonAsync($"/api/organizations/{other.Id}/roles",
            new CreateRoleRequest("Foreign", new[] { PermissionCatalogue.OrgRead }))).StatusCode);
        await using (var downgrade = _fixture.CreateContext())
        {
            var member = await downgrade.OrganizationMembers.IgnoreQueryFilters().SingleAsync(m => m.UserId == owner.Id);
            member.Role = OrganizationRole.Member;
            await downgrade.SaveChangesAsync();
        }
        // Keep the same JWT: its old Owner/perms claims must not authorize anything.
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync($"/api/organizations/{org.Id}/roles")).StatusCode);
        using var denied = await client.PostAsJsonAsync("/graphql", query);
        var errors = JsonDocument.Parse(await denied.Content.ReadAsStringAsync());
        Assert.NotEmpty(errors.RootElement.GetProperty("errors").EnumerateArray());
        using var refreshed = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = rawRefresh, organizationId = org.Id });
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var payload = JsonDocument.Parse(await refreshed.Content.ReadAsStringAsync());
        var newJwt = new JwtSecurityTokenHandler().ReadJwtToken(payload.RootElement.GetProperty("accessToken").GetString());
        Assert.Equal("member", newJwt.Claims.Single(c => c.Type == "role").Value);
        Assert.DoesNotContain(newJwt.Claims, c => c.Type == "perms" && c.Value.Contains(PermissionCatalogue.OrgRolesManage));
    }

    [SqlAuthFact]
    public async Task ForceRevoke_CountsFamiliesAndRevokesEverySession()
    {
        var (org, owner) = await SeedAsync();
        var role = new Role { OrganizationId = org.Id, Name = "Owner", IsSystem = true };
        await using (var setup = _fixture.CreateContext())
        {
            setup.Roles.Add(role);
            for (var i = 0; i < 2; i++)
                setup.RefreshTokens.Add(new RefreshToken
                {
                    UserId = owner.Id, FamilyId = Guid.NewGuid(), ExpiresAt = DateTime.UtcNow.AddDays(1),
                    TokenHash = Guid.NewGuid().ToString("N")
                });
            await setup.SaveChangesAsync();
        }
        await using var db = _fixture.CreateContext();
        var result = await Service(db, org.Id).ForceRevokeAsync(owner.Id, org.Id, role.Id);
        Assert.Equal(1, result.AffectedUsers);
        Assert.Equal(2, result.FamiliesRevoked);
        Assert.False(await db.RefreshTokens.AnyAsync(t => t.UserId == owner.Id && t.RevokedAt == null));
    }

    private sealed class ContextFactory(SqlAuthFixture fixture) : IDbContextFactory<GriotDbContext>
    {
        public GriotDbContext CreateDbContext() => fixture.CreateContext();
    }
}
