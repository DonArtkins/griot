using System.Security.Claims;
using Griot.Api.Auth;
using Griot.Api.GraphQL.Types;
using Griot.Infrastructure.Persistence;
using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Griot.Api.GraphQL;

public class GriotMutation
{
    /// <summary>
    /// True when the ClaimsPrincipal originated from the AI GRIOT_SERVICE_TOKEN
    /// On-Behalf-Of flow. Detected via the "ai-on-behalf-of" role or the explicit
    /// "auth_method=service_token_obo" claim. Used to gate destructive GraphQL
    /// mutations ON TOP OF the OBO user's own RBAC permissions — an Owner user
    /// acting through the AI path still cannot delete workspaces.
    /// </summary>
    private static bool IsAiCall(ClaimsPrincipal cp)
        => cp.IsInRole(ServiceTokenHandler.AiOnBehalfOfRole)
        || cp.HasClaim("auth_method", "service_token_obo");

    /// <summary>
    /// Create a new workspace
    /// </summary>
    [Authorize]
    public async Task<WorkspaceType> CreateWorkspace(
        string name,
        string slug,
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userGuid = AuthenticatedUserId(claimsPrincipal);

        var workspace = new Griot.Domain.Entities.Workspace
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            OwnerId = userGuid,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Workspaces.Add(workspace);
        await dbContext.SaveChangesAsync(cancellationToken);

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
    /// Update workspace details (workspace owner or member)
    /// </summary>
    [Authorize]
    public async Task<WorkspaceType> UpdateWorkspace(
        Guid id,
        string name,
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var workspace = await dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workspace == null)
            throw new ArgumentException("Workspace not found");

        // Caller must be workspace owner or member (CWE-862): never trust the id alone.
        await RequireWorkspaceAccessAsync(dbContext, workspace.Id, claimsPrincipal, cancellationToken);

        workspace.Name = name;
        workspace.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

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
    /// Delete a workspace (Owner only)
    /// </summary>
    [Authorize]
    public async Task<bool> DeleteWorkspace(
        Guid id,
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        if (IsAiCall(claimsPrincipal))
            throw new UnauthorizedAccessException("Destructive operations are not allowed via AI service-token OBO.");

        var workspace = await dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workspace == null)
            return false;

        // Owner only - delete is the most destructive operation.
        await RequireWorkspaceOwnerAsync(dbContext, workspace.Id, claimsPrincipal, cancellationToken);

        dbContext.Workspaces.Remove(workspace);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Create a new project in workspace
    /// </summary>
    [Authorize]
    public async Task<ProjectType> CreateProject(
        Guid workspaceId,
        string name,
        string? description,
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        await RequireWorkspaceAccessAsync(dbContext, workspaceId, claimsPrincipal, cancellationToken);

        var project = new Griot.Domain.Entities.Project
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name,
            Description = description,
            Status = Griot.Domain.Enums.ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectType
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            WorkspaceId = project.WorkspaceId,
            Status = project.Status,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }

    /// <summary>
    /// Update project details
    /// </summary>
    [Authorize]
    public async Task<ProjectType> UpdateProject(
        Guid id,
        string name,
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project == null)
            throw new ArgumentException("Project not found");

        await RequireWorkspaceAccessAsync(dbContext, project.WorkspaceId, claimsPrincipal, cancellationToken);

        project.Name = name;
        project.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProjectType
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            WorkspaceId = project.WorkspaceId,
            Status = project.Status,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }

    /// <summary>
    /// Delete a project
    /// </summary>
    [Authorize]
    public async Task<bool> DeleteProject(
        Guid id,
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        if (IsAiCall(claimsPrincipal))
            throw new UnauthorizedAccessException("Destructive operations are not allowed via AI service-token OBO.");

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (project == null)
            return false;

        // Owner/Admin only per api-surface.md (delete is destructive).
        await RequireWorkspaceAdminOrOwnerAsync(dbContext, project.WorkspaceId, claimsPrincipal, cancellationToken);

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Create a new board in project
    /// </summary>
    [Authorize]
    public async Task<BoardType> CreateBoard(
        Guid projectId,
        string name,
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId, cancellationToken);

        if (project == null)
            throw new ArgumentException("Project not found");

        await RequireWorkspaceAccessAsync(dbContext, project.WorkspaceId, claimsPrincipal, cancellationToken);

        var board = new Griot.Domain.Entities.Board
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Boards.Add(board);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BoardType
        {
            Id = board.Id,
            Name = board.Name,
            ProjectId = board.ProjectId,
            CreatedAt = board.CreatedAt
        };
    }

    /// <summary>
    /// Create a new task in column
    /// </summary>
    [Authorize]
    public async Task<TaskItemType> CreateTask(
        Guid columnId,
        string title,
        string? description,
        int priority,
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userGuid = AuthenticatedUserId(claimsPrincipal);

        // Reject out-of-range priorities before the enum cast reaches persistence (CWE-20).
        var priorityValue = ValidatePriority(priority);

        // Caller must belong to the workspace owning the column's board.
        var workspaceId = await ResolveColumnWorkspaceIdAsync(dbContext, columnId, cancellationToken);
        await RequireWorkspaceAccessAsync(dbContext, workspaceId, claimsPrincipal, cancellationToken);

        var task = new Griot.Domain.Entities.TaskItem
        {
            Id = Guid.NewGuid(),
            ColumnId = columnId,
            Title = title,
            Description = description,
            Status = Griot.Domain.Enums.TaskStatus.Backlog,
            Priority = priorityValue,
            CreatorId = userGuid,
            Position = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.TaskItems.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);

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
    /// Update task details
    /// </summary>
    [Authorize]
    public async Task<TaskItemType> UpdateTask(
        Guid id,
        string title,
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.TaskItems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task == null)
            throw new ArgumentException("Task not found");

        var workspaceId = await ResolveColumnWorkspaceIdAsync(dbContext, task.ColumnId, cancellationToken);
        await RequireWorkspaceAccessAsync(dbContext, workspaceId, claimsPrincipal, cancellationToken);

        task.Title = title;
        task.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

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
    /// Delete a task
    /// </summary>
    [Authorize]
    public async Task<bool> DeleteTask(
        Guid id,
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        if (IsAiCall(claimsPrincipal))
            throw new UnauthorizedAccessException("Destructive operations are not allowed via AI service-token OBO.");

        var task = await dbContext.TaskItems
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (task == null)
            return false;

        var workspaceId = await ResolveColumnWorkspaceIdAsync(dbContext, task.ColumnId, cancellationToken);
        await RequireWorkspaceAdminOrOwnerAsync(dbContext, workspaceId, claimsPrincipal, cancellationToken);

        dbContext.TaskItems.Remove(task);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Add a comment to a task
    /// </summary>
    [Authorize]
    public async Task<CommentType> AddComment(
        Guid taskId,
        string content,
        [Service] GriotDbContext dbContext,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        var userGuid = AuthenticatedUserId(claimsPrincipal);

        var task = await dbContext.TaskItems
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        if (task == null)
            throw new ArgumentException("Task not found");

        // Caller must belong to the workspace owning the task's board.
        var workspaceId = await ResolveColumnWorkspaceIdAsync(dbContext, task.ColumnId, cancellationToken);
        await RequireWorkspaceAccessAsync(dbContext, workspaceId, claimsPrincipal, cancellationToken);

        var comment = new Griot.Domain.Entities.Comment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            AuthorId = userGuid,
            Body = content,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Comments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CommentType
        {
            Id = comment.Id,
            TaskId = comment.TaskId,
            AuthorId = comment.AuthorId,
            Body = comment.Body,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }

    /// <summary>
    /// Resolve the authenticated user Guid from either JWT "sub" claim or the
    /// XML-SOAP NameIdentifier claim (used by the OBO ServiceToken handler).
    /// Throws <see cref="UnauthorizedAccessException"/> when neither is present
    /// on the request when the token has no usable subject claim.
    /// </summary>
    private static Guid AuthenticatedUserId(ClaimsPrincipal claimsPrincipal)
    {
        var userId = claimsPrincipal.FindFirst("sub")?.Value
                  ?? claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null || !Guid.TryParse(userId, out var userGuid))
            throw new UnauthorizedAccessException();

        return userGuid;
    }

    /// <summary>
    /// Validate a GraphQL priority argument (0..3 = Low..Urgent) before it is
    /// assigned to a TaskItem. Rejects out-of-range values that would otherwise
    /// produce an undefined enum instance reaching persistence (CWE-20).
    /// </summary>
    private static Griot.Domain.Enums.Priority ValidatePriority(int priority)
    {
        if (priority == 0)
            return Griot.Domain.Enums.Priority.Low;
        if (priority == 1)
            return Griot.Domain.Enums.Priority.Medium;
        if (priority == 2)
            return Griot.Domain.Enums.Priority.High;
        if (priority == 3)
            return Griot.Domain.Enums.Priority.Urgent;

        throw new ArgumentException($"Invalid priority value: {priority}");
    }

    /// <summary>
    /// Resolve a column's owning workspace id (Column -&gt; Board -&gt; Project) and
    /// throw when the chain is broken so existence is never disclosed.
    /// </summary>
    private async Task<Guid> ResolveColumnWorkspaceIdAsync(
        GriotDbContext dbContext,
        Guid columnId,
        CancellationToken cancellationToken)
    {
        var column = await dbContext.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId, cancellationToken);

        if (column == null)
            throw new ArgumentException("Column not found");

        var board = await dbContext.Boards
            .FirstOrDefaultAsync(b => b.Id == column.BoardId, cancellationToken);

        if (board == null)
            throw new ArgumentException("Board not found");

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == board.ProjectId, cancellationToken);

        if (project == null)
            throw new ArgumentException("Project not found");

        return project.WorkspaceId;
    }

    /// <summary>
    /// Workspace access check: the caller must be the workspace owner or a
    /// workspace member of any role (CWE-862).
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
            throw new ArgumentException("Workspace not found");

        if (workspace.OwnerId == userGuid)
            return;

        var member = await dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userGuid, cancellationToken);

        if (member == null)
            throw new UnauthorizedAccessException();
    }

    /// <summary>
    /// Workspace Owner/Admin gate: caller must be the workspace owner or a member
    /// with an Admin role. Members are rejected (destructive operations).
    /// </summary>
    private async Task RequireWorkspaceAdminOrOwnerAsync(
        GriotDbContext dbContext,
        Guid workspaceId,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        if (IsAiCall(claimsPrincipal))
            throw new UnauthorizedAccessException("Admin-or-owner destructive operations are not allowed via AI service-token OBO.");

        var userGuid = AuthenticatedUserId(claimsPrincipal);

        var workspace = await dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace == null)
            throw new ArgumentException("Workspace not found");

        if (workspace.OwnerId == userGuid)
            return;

        var member = await dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userGuid, cancellationToken);

        if (member == null || member.Role == Griot.Domain.Enums.WorkspaceRole.Member)
            throw new UnauthorizedAccessException();
    }

    /// <summary>
    /// Workspace Owner-only gate: only the workspace owner may pass (delete).
    /// </summary>
    private async Task RequireWorkspaceOwnerAsync(
        GriotDbContext dbContext,
        Guid workspaceId,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken)
    {
        if (IsAiCall(claimsPrincipal))
            throw new UnauthorizedAccessException("Owner-only destructive operations are not allowed via AI service-token OBO.");

        var userGuid = AuthenticatedUserId(claimsPrincipal);

        var workspace = await dbContext.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId, cancellationToken);

        if (workspace == null || workspace.OwnerId != userGuid)
            throw new UnauthorizedAccessException();
    }
}
