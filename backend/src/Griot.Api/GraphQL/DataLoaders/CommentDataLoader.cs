using Griot.Api.GraphQL.Types;
using Griot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Griot.Api.GraphQL.DataLoaders;

public class CommentDataLoader : GroupedDataLoader<Guid, CommentType>
{
    private readonly IDbContextFactory<GriotDbContext> _dbContextFactory;

    public CommentDataLoader(
        IBatchScheduler batchScheduler,
        IDbContextFactory<GriotDbContext> dbContextFactory,
        DataLoaderOptions? options = null)
        : base(batchScheduler, options ?? new DataLoaderOptions())
    {
        _dbContextFactory = dbContextFactory;
    }

    protected override async Task<ILookup<Guid, CommentType>> LoadGroupedBatchAsync(
        IReadOnlyList<Guid> taskIds,
        CancellationToken cancellationToken)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

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
