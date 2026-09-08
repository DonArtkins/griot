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
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var workspace = await dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        
        if (workspace == null)
            return null;

        // [Authorize] proves the caller has a token; workspace access itself is
        // enforced per-resolver (owner or member) before any data is returned.
        await RequireWorkspaceAccessAsync(dbContext, workspace.Id, claimsPrincipal, cancellationToken);

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
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal)
    {
        // Caller must be owner or member of the target workspace (CWE-862).
        RequireWorkspaceAccessSync(dbContext, workspaceId, AuthenticatedUserId(claimsPrincipal));

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
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var board = await dbContext.Boards
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (board == null)
            return null;

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == board.ProjectId, cancellationToken);

        if (project == null)
            return null;

        await RequireWorkspaceAccessAsync(dbContext, project.WorkspaceId, claimsPrincipal, cancellationToken);

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
    // Data-pipeline middleware must be declared in pipeline order:
    // UseDbContext -> UsePaging -> UseProjection -> UseFiltering -> UseSorting.
    [Authorize]
    [UsePaging(MaxPageSize = 1000, DefaultPageSize = 100)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<TaskItemType> GetTasks(
        Guid boardId,
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal)
    {
        // boardId is required: an unscoped task query would expose tasks across
        // every workspace. Access is enforced per board workspace (owner/member).
        RequireBoardAccessSync(dbContext, boardId, AuthenticatedUserId(claimsPrincipal));

        var columnIds = dbContext.Columns
            .Where(c => c.BoardId == boardId)
            .Select(c => c.Id);

        var query = dbContext.TaskItems
            .Where(t => columnIds.Contains(t.ColumnId));

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
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.TaskItems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task == null)
            return null;

        var workspaceId = await ResolveTaskWorkspaceIdAsync(dbContext, task.ColumnId, cancellationToken);
        await RequireWorkspaceAccessAsync(dbContext, workspaceId, claimsPrincipal, cancellationToken);

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
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.TaskItems
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task == null)
            return new List<CommentType>();

        var workspaceId = await ResolveTaskWorkspaceIdAsync(dbContext, task.ColumnId, cancellationToken);
        await RequireWorkspaceAccessAsync(dbContext, workspaceId, claimsPrincipal, cancellationToken);

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
    [UsePaging(MaxPageSize = 1000, DefaultPageSize = 100)]
    [UseFiltering]
    [UseSorting]
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
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal)
    {
        RequireWorkspaceAccessSync(dbContext, workspaceId, AuthenticatedUserId(claimsPrincipal));

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
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        await RequireWorkspaceAccessAsync(dbContext, workspaceId, claimsPrincipal, cancellationToken);

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

    // ------------------------------------------------------------------
    // Workspace authorization helpers (caller-scoped ownership/membership)
    // ------------------------------------------------------------------

    /// <summary>
    /// Resolve the authenticated user id from the JWT principal, or reject the
    /// request when the token has no usable subject claim.
    /// </summary>
    private static Guid AuthenticatedUserId(ClaimsPrincipal claimsPrincipal)
    {
        var userId = claimsPrincipal.FindFirst("sub")?.Value;
        if (userId == null || !Guid.TryParse(userId, out var userGuid))
            throw new UnauthorizedAccessException();

        return userGuid;
    }

    /// <summary>
    /// Resolve a task's owning workspace id via TaskItem -&gt; Column -&gt; Board -&gt; Project.
    /// Throws when the chain is broken so existence is never disclosed.
    /// </summary>
    private async Task<Guid> ResolveTaskWorkspaceIdAsync(
        GriotDbContext dbContext,
        Guid columnId,
        CancellationToken cancellationToken)
    {
        var column = await dbContext.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId, cancellationToken);

        if (column == null)
            throw new UnauthorizedAccessException();

        var board = await dbContext.Boards
            .FirstOrDefaultAsync(b => b.Id == column.BoardId, cancellationToken);

        if (board == null)
            throw new UnauthorizedAccessException();

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == board.ProjectId, cancellationToken);

        if (project == null)
            throw new UnauthorizedAccessException();

        return project.WorkspaceId;
    }

    /// <summary>
    /// Async workspace access check: the caller must be the workspace owner or a
    /// workspace member. Missing workspaces are treated as unauthorized so
    /// existence is never disclosed (CWE-862).
    /// </summary>
    private async Task RequireWorkspaceAccessAsync(
        GriotDbContext dbContext,
        Guid workspaceId,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userGuid = AuthenticatedUserId(claimsPrincipal);

        var workspace = await dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace == null)
            throw new UnauthorizedAccessException();

        if (workspace.OwnerId == userGuid)
            return;

        var member = await dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userGuid, cancellationToken);

        if (member == null)
            throw new UnauthorizedAccessException();
    }

    /// <summary>
    /// Synchronous twin of RequireWorkspaceAccessAsync for IQueryable resolvers
    /// (the paging/filtering/sorting middleware requires a synchronous IQueryable
    /// return). Uses scalar-existence queries only - no result-set loading.
    /// </summary>
    private static void RequireWorkspaceAccessSync(
        GriotDbContext dbContext,
        Guid workspaceId,
        Guid userGuid)
    {
        var isOwner = dbContext.Workspaces
            .Where(w => w.Id == workspaceId && w.OwnerId == userGuid)
            .Any();

        if (isOwner)
            return;

        var isMember = dbContext.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.UserId == userGuid)
            .Any();

        if (!isMember)
            throw new UnauthorizedAccessException();
    }

    /// <summary>
    /// Resolve a board's owning workspace id (Board -&gt; Project) and enforce
    /// workspace access. Missing boards are treated as unauthorized.
    /// </summary>
    private static void RequireBoardAccessSync(
        GriotDbContext dbContext,
        Guid boardId,
        Guid userGuid)
    {
        var workspaceId = dbContext.Boards
            .Join(dbContext.Projects, b => b.ProjectId, p => p.Id, (b, p) => new { Board = b, Project = p })
            .Where(x => x.Board.Id == boardId)
            .Select(x => x.Project.WorkspaceId)
            .FirstOrDefault();

        if (workspaceId == Guid.Empty)
            throw new UnauthorizedAccessException();

        RequireWorkspaceAccessSync(dbContext, workspaceId, userGuid);
    }
}
