using System;
using Griot.Application.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Griot.Infrastructure.Persistence;

/// <summary>
/// Spec 29 helper: copies the active scope from <see cref="ITenantContext"/>
/// into a freshly created <see cref="GriotDbContext"/> (global query filters
/// read <see cref="GriotDbContext.TenantId"/>). The pooled-factory wrapper in
/// `Program.cs` calls this for every scoped context; repositories may also
/// call <see cref="GriotDbContext.TenantId"/> directly in tests.
/// </summary>
public static class TenantDbContextExtensions
{
    public static GriotDbContext WithTenant(this GriotDbContext context, ITenantContext tenant)
    {
        context.TenantId = tenant.OrganizationId;
        return context;
    }
}
