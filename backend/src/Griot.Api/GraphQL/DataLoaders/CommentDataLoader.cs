using Griot.Api.GraphQL.Types;
using Griot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Griot.Api.GraphQL.DataLoaders;

public class CommentDataLoader : GroupedDataLoader<Guid, CommentType>
{
    private readonly IDbContextFactory<GriotDbContext> _dbContextFactory;
    private readonly Griot.Application.Tenancy.ITenantContext _tenantContext;

    public CommentDataLoader(
        IBatchScheduler batchScheduler,
        IDbContextFactory<GriotDbContext> dbContextFactory,
        Griot.Application.Tenancy.ITenantContext tenantContext,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options ?? new DataLoaderOptions())
    {
        _dbContextFactory = dbContextFactory;
        _tenantContext = tenantContext;
    }

    protected override async Task<ILookup<Guid, CommentType>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> taskIds,
        CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        // Spec 29: copy the ambient tenant scope — batched comment reads are
        // filtered to the caller's org exactly like the scoped request context.
        context.WithTenant(_tenantContext);

        var comments = await context.Comments
            .Where(c => taskIds.Contains(c.TaskId))
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return comments
            .Select(c => new CommentType
            {
                Id = c.Id,
                TaskId = c.TaskId,
                AuthorId = c.AuthorId,
                Body = c.Body,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToLookup(c => c.TaskId);
    }
}
