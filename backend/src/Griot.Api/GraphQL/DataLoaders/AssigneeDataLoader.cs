using Griot.Api.GraphQL.Types;
using Griot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Griot.Api.GraphQL.DataLoaders;

public class AssigneeDataLoader : BatchDataLoader<Guid, UserType>
{
    private readonly IDbContextFactory<GriotDbContext> _dbContextFactory;

    public AssigneeDataLoader(
        IBatchScheduler batchScheduler,
        IDbContextFactory<GriotDbContext> dbContextFactory,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options)
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<IReadOnlyDictionary<Guid, UserType>> LoadBatchAsync(
        IReadOnlyList<Guid> keys,
        CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

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
