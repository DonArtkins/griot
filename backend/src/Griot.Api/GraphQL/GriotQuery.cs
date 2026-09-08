using System.Security.Claims;
using Griot.Api.GraphQL.Types;
using Griot.Application.Interfaces.Services;
using Griot.Infrastructure.Persistence;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Griot.Api.GraphQL;

public class GriotQuery
{
    /// <summary>
    /// Get the currently authenticated user
    /// </summary>
    [Authorize]
    public async Task<UserType?> GetMe(
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.FindFirst("sub")?.Value;
        if (userId == null || !Guid.TryParse(userId, out var userGuid))
            return null;

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userGuid, cancellationToken);

        if (user == null)
            return null;

        return new UserType
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            TwoFactorMethod = user.TwoFactorMethod,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Get a specific workspace by ID
    /// </summary>
    [Authorize]
    public async Task<WorkspaceType?> GetWorkspace(
        Guid id,
        [Service] GriotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var workspace = await dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        
        if (workspace == null)
            return null;

        return new WorkspaceType
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Slug = workspace.Slug,
            OwnerId = workspace.OwnerId,
            CreatedAt = workspace.CreatedAt,
            UpdatedAt = workspace.UpdatedAt
        };
    }

    /// <summary>
    /// Get all projects in a workspace
    /// </summary>
    [Authorize]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ProjectType> GetProjects(
        Guid workspaceId,
        [Service] GriotDbContext dbContext)
    {
        return dbContext.Projects
            .Where(p => p.WorkspaceId == workspaceId)
            .Select(p => new ProjectType
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                WorkspaceId = p.WorkspaceId,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            });
    }

    /// <summary>
    /// Get a specific board by ID with its columns and tasks
    /// </summary>
    [Authorize]
    public async Task<BoardType?> GetBoard(
        Guid id,
        [Service] GriotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var board = await dbContext.Boards
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (board == null)
            return null;

        return new BoardType
        {
            Id = board.Id,
            Name = board.Name,
            ProjectId = board.ProjectId,
            CreatedAt = board.CreatedAt
        };
    }

    /// <summary>
    /// Get tasks with filtering and sorting
    /// </summary>
    [Authorize]
    [UseFiltering]
    [UseSorting]
    [UsePaging(MaxPageSize = 1000, DefaultPageSize = 100)]
    public IQueryable<TaskItemType> GetTasks(
        Guid? boardId,
        [Service] GriotDbContext dbContext)
    {
        var query = dbContext.TaskItems.AsQueryable();

        if (boardId.HasValue)
        {
            var columnIds = dbContext.Columns
                .Where(c => c.BoardId == boardId.Value)
                .Select(c => c.Id);

            query = query.Where(t => columnIds.Contains(t.ColumnId));
        }

        return query.Select(t => new TaskItemType
        {
            Id = t.Id,
            ColumnId = t.ColumnId,
            Title = t.Title,
            Description = t.Description,
            Status = t.Status,
            Priority = t.Priority,
            AssigneeId = t.AssigneeId,
            CreatorId = t.CreatorId,
            DueDate = t.DueDate,
            Position = t.Position,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        });
    }

    /// <summary>
    /// Get a specific task by ID
    /// </summary>
    [Authorize]
    public async Task<TaskItemType?> GetTask(
        Guid id,
        [Service] GriotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.TaskItems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task == null)
            return null;

        return new TaskItemType
        {
            Id = task.Id,
            ColumnId = task.ColumnId,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            AssigneeId = task.AssigneeId,
            CreatorId = task.CreatorId,
            DueDate = task.DueDate,
            Position = task.Position,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }

    /// <summary>
    /// Get comments for a specific task
    /// </summary>
    [Authorize]
    public async Task<List<CommentType>> GetComments(
        Guid taskId,
        [Service] GriotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var comments = await dbContext.Comments
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return comments.Select(c => new CommentType
        {
            Id = c.Id,
            TaskId = c.TaskId,
            AuthorId = c.AuthorId,
            Body = c.Body,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();
    }

    /// <summary>
    /// Get notifications for the current user
    /// </summary>
    [Authorize]
    [UseFiltering]
    [UseSorting]
    [UsePaging(MaxPageSize = 1000, DefaultPageSize = 100)]
    public IQueryable<NotificationGraphQLType> GetNotifications(
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal)
    {
        var userId = claimsPrincipal.FindFirst("sub")?.Value;
        if (userId == null || !Guid.TryParse(userId, out var userGuid))
            return Enumerable.Empty<NotificationGraphQLType>().AsQueryable();

        return dbContext.Notifications
            .Where(n => n.UserId == userGuid)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationGraphQLType
            {
                Id = n.Id,
                UserId = n.UserId,
                Type = n.Type,
                Title = n.Title,
                Body = n.Body,
                TargetRef = n.TargetRef,
                ReadAt = n.ReadAt,
                CreatedAt = n.CreatedAt
            });
    }

    /// <summary>
    /// Get unread notification count for the current user
    /// </summary>
    [Authorize]
    public async Task<NotificationCountType> GetUnreadNotificationCount(
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userId = claimsPrincipal.FindFirst("sub")?.Value;
        if (userId == null || !Guid.TryParse(userId, out var userGuid))
            return new NotificationCountType { Count = 0 };

        var count = await dbContext.Notifications
            .Where(n => n.UserId == userGuid && n.ReadAt == null)
            .CountAsync(cancellationToken);

        return new NotificationCountType { Count = count };
    }

    /// <summary>
    /// Get activity feed for a workspace
    /// </summary>
    [Authorize]
    [UsePaging(MaxPageSize = 1000, DefaultPageSize = 100)]
    public IQueryable<ActivityLogType> GetActivityFeed(
        Guid workspaceId,
        [Service] GriotDbContext dbContext)
    {
        return dbContext.ActivityLogs
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ActivityLogType
            {
                Id = a.Id,
                WorkspaceId = a.WorkspaceId,
                ActorId = a.ActorId,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Action = a.Action,
                Payload = a.Payload,
                CreatedAt = a.CreatedAt
            });
    }

    /// <summary>
    /// Get dashboard summary for a workspace
    /// </summary>
    [Authorize]
    public async Task<DashboardSummaryType> GetDashboardSummary(
        Guid workspaceId,
        [Service] GriotDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // This will be enhanced with the stored procedure in a later phase
        var tasks = await dbContext.TaskItems
            .Join(
                dbContext.Columns,
                t => t.ColumnId,
                c => c.Id,
                (t, c) => new { Task = t, Column = c })
            .Join(
                dbContext.Boards,
                tc => tc.Column.BoardId,
                b => b.Id,
                (tc, b) => new { tc.Task, tc.Column, Board = b })
            .Join(
                dbContext.Projects,
                tcb => tcb.Board.ProjectId,
                p => p.Id,
                (tcb, p) => new { tcb.Task, tcb.Column, tcb.Board, Project = p })
            .Where(x => x.Project.WorkspaceId == workspaceId)
            .Select(x => new { x.Task.Status, x.Task.Priority })
            .ToListAsync(cancellationToken);

        var taskCountsByStatus = tasks
            .GroupBy(t => t.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var urgentOpenCount = tasks
            .Count(t => t.Priority == Domain.Enums.Priority.Urgent && 
                       t.Status != Domain.Enums.TaskStatus.Done);

        var recentActivity = await dbContext.ActivityLogs
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new ActivityLogType
            {
                Id = a.Id,
                WorkspaceId = a.WorkspaceId,
                ActorId = a.ActorId,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Action = a.Action,
                Payload = a.Payload,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new DashboardSummaryType
        {
            TaskCountsByStatus = taskCountsByStatus,
            UrgentOpenCount = urgentOpenCount,
            RecentActivity = recentActivity
        };
    }
}
