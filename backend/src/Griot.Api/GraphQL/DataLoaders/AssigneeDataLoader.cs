using Griot.Api.GraphQL.Types;
using Griot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Griot.Api.GraphQL.DataLoaders;

public class AssigneeDataLoader : BatchDataLoader<Guid, UserType>
{
    private readonly IDbContextFactory<GriotDbContext> _dbContextFactory;
    private readonly Griot.Application.Tenancy.ITenantContext _tenantContext;

    public AssigneeDataLoader(
        IBatchScheduler batchScheduler,
        IDbContextFactory<GriotDbContext> dbContextFactory,
        Griot.Application.Tenancy.ITenantContext tenantContext,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options ?? new DataLoaderOptions())
    {
        _dbContextFactory = dbContextFactory;
        _tenantContext = tenantContext;
    }

    protected override async Task<IReadOnlyDictionary<Guid, UserType>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        // Spec 29: DataLoader contexts are created straight from the pooled factory,
        // bypassing the scoped-factory wrapper — copy the ambient tenant scope here
        // so batched reads never see another org's rows.
        context.WithTenant(_tenantContext);

        var users = await context.Users
            .Where(u => keys.Contains(u.Id))
            .ToListAsync(cancellationToken);

        return users.ToDictionary(
            u => u.Id,
            u => new UserType
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                TwoFactorMethod = u.TwoFactorMethod,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt
            });
    }
}
