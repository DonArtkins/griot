using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;
using Griot.Application.Interfaces.Repositories;
using Griot.Domain.Entities;
using Griot.Domain.Enums;

namespace Griot.Application.Services;

/// <summary>
/// Consolidated implementation of the full REST domain surface (specs 13–17), all
/// behind <see cref="IGenericRepository{T}"/> so the application layer stays
/// EF-free. Controllers extract the JWT sub and map <see cref="DomainError"/>.
/// </summary>
public class DomainService : IDomainService
{
    private readonly IGenericRepository<User> _users;
    private readonly IGenericRepository<Workspace> _workspaces;
    private readonly IGenericRepository<WorkspaceMember> _members;
    private readonly IGenericRepository<Invite> _invites;
    private readonly IGenericRepository<Project> _projects;
    private readonly IGenericRepository<Board> _boards;
    private readonly IGenericRepository<Column> _columns;
    private readonly IGenericRepository<TaskItem> _tasks;
    private readonly IGenericRepository<Comment> _comments;
    private readonly IGenericRepository<Attachment> _attachments;
    private readonly IGenericRepository<Notification> _notifications;
    private readonly IGenericRepository<ActivityLog> _activity;
    private readonly IGenericRepository<ErrorLog> _errors;
    private readonly IGenericRepository<AuditLog> _audit;

    public DomainService(
        IGenericRepository<User> users,
        IGenericRepository<Workspace> workspaces,
        IGenericRepository<WorkspaceMember> members,
        IGenericRepository<Invite> invites,
        IGenericRepository<Project> projects,
        IGenericRepository<Board> boards,
        IGenericRepository<Column> columns,
        IGenericRepository<TaskItem> tasks,
        IGenericRepository<Comment> comments,
        IGenericRepository<Attachment> attachments,
        IGenericRepository<Notification> notifications,
        IGenericRepository<ActivityLog> activity,
        IGenericRepository<ErrorLog> errors,
        IGenericRepository<AuditLog> audit)
    {
        _users = users; _workspaces = workspaces; _members = members; _invites = invites;
        _projects = projects; _boards = boards; _columns = columns; _tasks = tasks;
        _comments = comments; _attachments = attachments; _notifications = notifications;
        _activity = activity; _errors = errors; _audit = audit;
    }

    // Attachment limits (spec 11 / api-surface "Attachment limits"): enforced in the
    // application layer before any persistence — 25 MB inclusive, MIME allowlist.
    public const long MaxAttachmentSizeBytes = 26_214_400; // 25 MB (inclusive upper bound)

    public static readonly string[] AllowedAttachmentMimeTypes =
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document", // .docx
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"        // .xlsx
    };

    // ══════════════════════ WORKSPACES ══════════════════════

    public async Task<List<WorkspaceDto>> GetWorkspacesAsync(Guid userId)
    {
        var mine = (await _workspaces.GetAllAsync())
            .Where(w => IsMember(w, userId))
            .ToList();
        return mine.Select(ToWorkspaceDto).ToList();
    }

    public async Task<WorkspaceDto> CreateWorkspaceAsync(CreateWorkspaceRequest request, Guid ownerId)
    {
        RequireText(request.Name, "Name is required.");
        var slug = string.IsNullOrWhiteSpace(request.Slug) ? Slugify(request.Name) : request.Slug.Trim();
        if ((await _workspaces.GetAllAsync()).Any(w => w.Slug == slug))
            throw new DomainError(DomainErrorKind.Conflict, "Slug is already in use.");

        var now = DateTime.UtcNow;
        var ws = new Workspace
        {
            Id = Guid.NewGuid(), Name = request.Name.Trim(), Slug = slug, OwnerId = ownerId,
            CreatedAt = now, UpdatedAt = now
        };
        await _workspaces.AddAsync(ws);
        await _members.AddAsync(new WorkspaceMember
        {
            WorkspaceId = ws.Id, UserId = ownerId, Role = WorkspaceRole.Owner, JoinedAt = now
        });
        await _workspaces.SaveChangesAsync();
        return ToWorkspaceDto(ws);
    }

    public async Task<WorkspaceDto?> GetWorkspaceAsync(Guid id, Guid userId)
    {
        var ws = await ScopedWorkspaceAsync(id, userId);
        return ws is null ? null : ToWorkspaceDto(ws);
    }

    public async Task<WorkspaceDto?> UpdateWorkspaceAsync(Guid id, UpdateWorkspaceRequest request, Guid userId)
    {
        var ws = await ScopedWorkspaceAsync(id, userId);
        if (ws is null) throw new DomainError(DomainErrorKind.NotFound, "Workspace not found.");
        if (!string.IsNullOrWhiteSpace(request.Name)) ws.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            if ((await _workspaces.GetAllAsync()).Any(w => w.Slug == request.Slug && w.Id != id))
                throw new DomainError(DomainErrorKind.Conflict, "Slug is already in use.");
            ws.Slug = request.Slug.Trim();
        }
        ws.UpdatedAt = DateTime.UtcNow;
        _workspaces.Update(ws);
        await _workspaces.SaveChangesAsync();
        return ToWorkspaceDto(ws);
    }

    public async Task<bool> DeleteWorkspaceAsync(Guid id, Guid userId)
    {
        var ws = await ScopedWorkspaceAsync(id, userId);
        if (ws is null) throw new DomainError(DomainErrorKind.NotFound, "Workspace not found.");
        if (ws.OwnerId != userId)
            throw new DomainError(DomainErrorKind.Forbidden, "Only the workspace owner can delete it.");
        _workspaces.Remove(ws);
        await _workspaces.SaveChangesAsync();
        return true;
    }

    public async Task<List<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, Guid userId)
    {
        await ScopedWorkspaceAsync(workspaceId, userId);
        return (await _members.GetAllAsync())
            .Where(m => m.WorkspaceId == workspaceId)
            .Select(ToMemberDto).ToList();
    }

    public async Task<WorkspaceMemberDto?> AddMemberAsync(Guid workspaceId, AddMemberRequest request, Guid userId)
    {
        var ws = await ScopedWorkspaceAsync(workspaceId, userId);
        if (ws is null) throw new DomainError(DomainErrorKind.Forbidden, "Not a member of this workspace.");
        if (!CanManageMembers(ws, userId))
            throw new DomainError(DomainErrorKind.Forbidden, "Owner/Admin required to add members.");
        if (await _users.GetByIdAsync(request.UserId) is null)
            throw new DomainError(DomainErrorKind.NotFound, "User not found.");
        if ((await _members.GetAllAsync()).Any(m => m.WorkspaceId == workspaceId && m.UserId == request.UserId))
            throw new DomainError(DomainErrorKind.Conflict, "User is already a member.");
        var member = new WorkspaceMember
        {
            WorkspaceId = workspaceId, UserId = request.UserId, Role = request.Role, JoinedAt = DateTime.UtcNow
        };
        await _members.AddAsync(member);
        await _members.SaveChangesAsync();
        return ToMemberDto(member);
    }

    public async Task<WorkspaceMemberDto?> UpdateMemberRoleAsync(Guid workspaceId, Guid memberUserId, UpdateMemberRoleRequest request, Guid userId)
    {
        var ws = await ScopedWorkspaceAsync(workspaceId, userId);
        if (ws is null) throw new DomainError(DomainErrorKind.Forbidden, "Not a member of this workspace.");
        if (!CanManageMembers(ws, userId))
            throw new DomainError(DomainErrorKind.Forbidden, "Owner/Admin required to update member roles.");
        var member = (await _members.GetAllAsync()).FirstOrDefault(m => m.WorkspaceId == workspaceId && m.UserId == memberUserId);
        if (member is null) return null;
        member.Role = request.Role;
        _members.Update(member);
        await _members.SaveChangesAsync();
        return ToMemberDto(member);
    }

    public async Task<bool> RemoveMemberAsync(Guid workspaceId, Guid memberUserId, Guid userId)
    {
        var ws = await ScopedWorkspaceAsync(workspaceId, userId);
        if (ws is null) throw new DomainError(DomainErrorKind.Forbidden, "Not a member of this workspace.");
        if (ws.OwnerId != userId)
            throw new DomainError(DomainErrorKind.Forbidden, "Only the workspace owner can remove members.");
        var member = (await _members.GetAllAsync()).FirstOrDefault(m => m.WorkspaceId == workspaceId && m.UserId == memberUserId);
        if (member is null) return false;
        _members.Remove(member);
        await _members.SaveChangesAsync();
        return true;
    }

    public async Task<InviteDto?> CreateInviteAsync(Guid workspaceId, CreateInviteRequest request, Guid userId)
    {
        var ws = await ScopedWorkspaceAsync(workspaceId, userId);
        if (ws is null) throw new DomainError(DomainErrorKind.Forbidden, "Not a member of this workspace.");
        if (!CanManageMembers(ws, userId))
            throw new DomainError(DomainErrorKind.Forbidden, "Owner/Admin required to invite.");
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new DomainError(DomainErrorKind.Validation, "Email is required.");
        var invite = new Invite
        {
            Id = Guid.NewGuid(), WorkspaceId = workspaceId, Email = request.Email.Trim().ToLowerInvariant(),
            Role = request.Role, Token = Guid.NewGuid().ToString().Replace("-", "") + Guid.NewGuid().ToString("N"),
            Status = InviteStatus.Pending, InvitedById = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedAt = DateTime.UtcNow
        };
        await _invites.AddAsync(invite);
        await _invites.SaveChangesAsync();
        return ToInviteDto(invite);
    }

    public async Task<bool> AcceptInviteAsync(string token, Guid userId)
    {
        var invite = (await _invites.GetAllAsync()).FirstOrDefault(i => i.Token == token);
        if (invite is null) throw new DomainError(DomainErrorKind.NotFound, "Invite not found or already used.");
        if (invite.Status == InviteStatus.Accepted || invite.ExpiresAt < DateTime.UtcNow)
            throw new DomainError(DomainErrorKind.Conflict, "Invite is not valid.");
        if (invite.Email.ToLowerInvariant() != (await _users.GetByIdAsync(userId))?.Email?.ToLowerInvariant())
            throw new DomainError(DomainErrorKind.Forbidden, "Invite is for a different email address.");
        if ((await _members.GetAllAsync()).Any(m => m.WorkspaceId == invite.WorkspaceId && m.UserId == userId))
            throw new DomainError(DomainErrorKind.Conflict, "Already a member.");
        await _members.AddAsync(new WorkspaceMember
        {
            WorkspaceId = invite.WorkspaceId, UserId = userId, Role = invite.Role, JoinedAt = DateTime.UtcNow
        });
        invite.Status = InviteStatus.Accepted;
        _invites.Update(invite);
        await _invites.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsUserMemberAsync(Guid workspaceId, Guid userId)
    {
        var ws = await _workspaces.GetByIdAsync(workspaceId);
        return ws is not null && IsMember(ws, userId);
    }

    // ══════════════════════ PROJECTS ══════════════════════

    public async Task<List<ProjectDto>> GetProjectsAsync(Guid workspaceId, Guid userId)
    {
        await ScopedWorkspaceAsync(workspaceId, userId);
        return (await _projects.GetAllAsync())
            .Where(p => p.WorkspaceId == workspaceId)
            .Select(ToProjectDto).ToList();
    }

    public async Task<ProjectDto> CreateProjectAsync(Guid workspaceId, CreateProjectRequest request, Guid userId)
    {
        await ScopedWorkspaceAsync(workspaceId, userId);
        RequireText(request.Name, "Name is required.");
        var project = new Project
        {
            Id = Guid.NewGuid(), WorkspaceId = workspaceId, Name = request.Name.Trim(),
            Key = string.IsNullOrWhiteSpace(request.Key) ? Slugify(request.Name).Substring(0, Math.Min(6, request.Name.Length)) : request.Key.Trim(),
            Description = request.Description, Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        await _projects.AddAsync(project);
        await _projects.SaveChangesAsync();
        return ToProjectDto(project);
    }

    public async Task<ProjectDto?> GetProjectAsync(Guid id, Guid userId)
    {
        var project = await _projects.GetByIdAsync(id);
        if (project is null) return null;
        await ScopedWorkspaceAsync(project.WorkspaceId, userId);
        return ToProjectDto(project);
    }

    public async Task<ProjectDto?> UpdateProjectAsync(Guid id, UpdateProjectRequest request, Guid userId)
    {
        var project = await _projects.GetByIdAsync(id);
        if (project is null) throw new DomainError(DomainErrorKind.NotFound, "Project not found.");
        await ScopedWorkspaceAsync(project.WorkspaceId, userId);
        if (!string.IsNullOrWhiteSpace(request.Name)) project.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Key)) project.Key = request.Key.Trim();
        if (request.Description is not null) project.Description = request.Description;
        if (request.Status is not null) { var v = request.Status; project.Status = v ?? ProjectStatus.Active; };
        project.UpdatedAt = DateTime.UtcNow;
        _projects.Update(project);
        await _projects.SaveChangesAsync();
        return ToProjectDto(project);
    }

    public async Task<bool> DeleteProjectAsync(Guid id, Guid userId)
    {
        var project = await _projects.GetByIdAsync(id);
        if (project is null) throw new DomainError(DomainErrorKind.NotFound, "Project not found.");
        var ws = await ScopedWorkspaceAsync(project.WorkspaceId, userId);
        if (ws is null) throw new DomainError(DomainErrorKind.Forbidden, "Not a member of this workspace.");
        if (!CanManageMembers(ws, userId))
            throw new DomainError(DomainErrorKind.Forbidden, "Owner/Admin required to delete projects.");
        _projects.Remove(project);
        await _projects.SaveChangesAsync();
        return true;
    }

    // ══════════════════════ BOARDS & COLUMNS ══════════════════════

    public async Task<List<BoardDto>> GetBoardsAsync(Guid projectId, Guid userId)
    {
        var project = await _projects.GetByIdAsync(projectId);
        if (project is null) throw new DomainError(DomainErrorKind.NotFound, "Project not found.");
        await ScopedWorkspaceAsync(project.WorkspaceId, userId);
        return (await _boards.GetAllAsync())
            .Where(b => b.ProjectId == projectId)
            .Select(ToBoardDto).ToList();
    }

    public async Task<BoardDto> CreateBoardAsync(Guid projectId, CreateBoardRequest request, Guid userId)
    {
        var project = await _projects.GetByIdAsync(projectId);
        if (project is null) throw new DomainError(DomainErrorKind.NotFound, "Project not found.");
        await ScopedWorkspaceAsync(project.WorkspaceId, userId);
        RequireText(request.Name, "Name is required.");
        var boardOrders = (await _boards.GetAllAsync()).Where(b => b.ProjectId == projectId).Select(b => b.Order).ToList();
        var maxOrder = ComputeMaxInt(boardOrders);
        var board = new Board
        {
            Id = Guid.NewGuid(), ProjectId = projectId, Name = request.Name.Trim(),
            Order = request.Order > 0 ? request.Order : maxOrder + 1, CreatedAt = DateTime.UtcNow
        };
        await _boards.AddAsync(board);
        await _boards.SaveChangesAsync();
        return ToBoardDto(board);
    }

    public async Task<BoardDto?> GetBoardAsync(Guid id, Guid userId)
    {
        var board = await _boards.GetByIdAsync(id);
        if (board is null) return null;
        var project = await _projects.GetByIdAsync(board.ProjectId);
        if (project is null) return null;
        await ScopedWorkspaceAsync(project.WorkspaceId, userId);
        return ToBoardDto(board);
    }

    public async Task<ColumnDto?> CreateColumnAsync(Guid boardId, CreateColumnRequest request, Guid userId)
    {
        var board = await _boards.GetByIdAsync(boardId);
        if (board is null) throw new DomainError(DomainErrorKind.NotFound, "Board not found.");
        var project = await _projects.GetByIdAsync(board.ProjectId);
        var ws = await ScopedWorkspaceAsync(project!.WorkspaceId, userId);
        if (ws is null) throw new DomainError(DomainErrorKind.Forbidden, "Not a member.");
        if (!CanManageMembers(ws, userId))
            throw new DomainError(DomainErrorKind.Forbidden, "Owner/Admin required to create columns.");
        RequireText(request.Name, "Name is required.");
        var columnOrders = (await _columns.GetAllAsync()).Where(c => c.BoardId == boardId).Select(c => c.Order).ToList();
        var maxOrder = ComputeMaxInt(columnOrders);
        var column = new Column
        {
            Id = Guid.NewGuid(), BoardId = boardId, Name = request.Name.Trim(),
            Order = request.Order > 0 ? request.Order : maxOrder + 1,
            WipLimit = request.WipLimit, CreatedAt = DateTime.UtcNow
        };
        await _columns.AddAsync(column);
        await _columns.SaveChangesAsync();
        return ToColumnDto(column);
    }

    public async Task<ColumnDto?> UpdateColumnAsync(Guid id, UpdateColumnRequest request, Guid userId)
    {
        var column = await _columns.GetByIdAsync(id);
        if (column is null) throw new DomainError(DomainErrorKind.NotFound, "Column not found.");
        var board = await _boards.GetByIdAsync(column.BoardId);
        var project = await _projects.GetByIdAsync(board!.ProjectId);
        var ws = await ScopedWorkspaceAsync(project!.WorkspaceId, userId);
        if (ws is null) throw new DomainError(DomainErrorKind.Forbidden, "Not a member.");
        if (!CanManageMembers(ws, userId))
            throw new DomainError(DomainErrorKind.Forbidden, "Owner/Admin required to update columns.");
        if (!string.IsNullOrWhiteSpace(request.Name)) column.Name = request.Name.Trim();
        if (request.Order is not null) { var v = request.Order; column.Order = v ?? 0; }
        if (request.WipLimit is not null) column.WipLimit = request.WipLimit;
        _columns.Update(column);
        await _columns.SaveChangesAsync();
        return ToColumnDto(column);
    }

    public async Task<bool> DeleteColumnAsync(Guid id, Guid userId)
    {
        var column = await _columns.GetByIdAsync(id);
        if (column is null) throw new DomainError(DomainErrorKind.NotFound, "Column not found.");
        var board = await _boards.GetByIdAsync(column.BoardId);
        var project = await _projects.GetByIdAsync(board!.ProjectId);
        var ws = await ScopedWorkspaceAsync(project!.WorkspaceId, userId);
        if (ws is null) throw new DomainError(DomainErrorKind.Forbidden, "Not a member.");
        if (!CanManageMembers(ws, userId))
            throw new DomainError(DomainErrorKind.Forbidden, "Owner/Admin required to delete columns.");
        _columns.Remove(column);
        await _columns.SaveChangesAsync();
        return true;
    }

    // ══════════════════════ TASKS ══════════════════════

    private async Task<bool> TaskVisibleAsync(TaskItem task, Guid userId)
    {
        var column = await _columns.GetByIdAsync(task.ColumnId);
        var board = column is null ? null : await _boards.GetByIdAsync(column.BoardId);
        var project = board is null ? null : await _projects.GetByIdAsync(board.ProjectId);
        return project is not null && await IsUserMemberAsync(project.WorkspaceId, userId);
    }

    public async Task<List<TaskItemDto>> GetTasksAsync(Guid boardId, Guid userId)
    {
        var board = await _boards.GetByIdAsync(boardId);
        if (board is null) throw new DomainError(DomainErrorKind.NotFound, "Board not found.");
        var project = await _projects.GetByIdAsync(board.ProjectId);
        await ScopedWorkspaceAsync(project!.WorkspaceId, userId);
        var columnIds = (await _columns.GetAllAsync()).Where(c => c.BoardId == boardId).Select(c => c.Id).ToList();
        return (await _tasks.GetAllAsync())
            .Where(t => columnIds.Contains(t.ColumnId))
            .Select(ToTaskDto).ToList();
    }

    public async Task<TaskItemDto?> CreateTaskAsync(Guid boardId, CreateTaskRequest request, Guid userId)
    {
        var board = await _boards.GetByIdAsync(boardId);
        if (board is null) throw new DomainError(DomainErrorKind.NotFound, "Board not found.");
        var project = await _projects.GetByIdAsync(board.ProjectId);
        await ScopedWorkspaceAsync(project!.WorkspaceId, userId);
        var column = await _columns.GetByIdAsync(request.ColumnId);
        if (column is null || column.BoardId != boardId)
            throw new DomainError(DomainErrorKind.Validation, "Column must belong to the board.");
        RequireText(request.Title, "Title is required.");
        var task = new TaskItem
        {
            Id = Guid.NewGuid(), BoardId = boardId, ColumnId = request.ColumnId, Title = request.Title.Trim(),
            Description = request.Description, Status = request.Status, Priority = request.Priority,
            AssigneeId = request.AssigneeId, CreatorId = userId, DueDate = request.DueDate,
            Position = request.Position, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        await _tasks.AddAsync(task);
        await _tasks.SaveChangesAsync();
        return ToTaskDto(task);
    }

    public async Task<TaskItemDto?> GetTaskAsync(Guid id, Guid userId)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task is null) return null;
        if (!await TaskVisibleAsync(task, userId)) return null;
        return ToTaskDto(task);
    }

    public async Task<TaskItemDto?> UpdateTaskAsync(Guid id, UpdateTaskRequest request, Guid userId)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task is null) throw new DomainError(DomainErrorKind.NotFound, "Task not found.");
        if (!await TaskVisibleAsync(task, userId)) throw new DomainError(DomainErrorKind.Forbidden, "Not a member of the containing workspace.");
        if (!string.IsNullOrWhiteSpace(request.Title)) task.Title = request.Title.Trim();
        if (request.Description is not null) task.Description = request.Description;
        if (request.Status is not null) { var v = request.Status; task.Status = v ?? Griot.Domain.Enums.TaskStatus.Backlog; };
        if (request.Priority is not null) { var v = request.Priority; task.Priority = v ?? Priority.Medium; };
        if (request.AssigneeId is not null) task.AssigneeId = request.AssigneeId;
        if (request.DueDate is not null) task.DueDate = request.DueDate;
        if (request.Position is not null) { var v = request.Position; task.Position = v ?? 0; };
        task.UpdatedAt = DateTime.UtcNow;
        _tasks.Update(task);
        await _tasks.SaveChangesAsync();
        return ToTaskDto(task);
    }

    public async Task<bool> DeleteTaskAsync(Guid id, Guid userId)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task is null) throw new DomainError(DomainErrorKind.NotFound, "Task not found.");
        if (!await TaskVisibleAsync(task, userId)) throw new DomainError(DomainErrorKind.Forbidden, "Not a member of the containing workspace.");
        _tasks.Remove(task);
        await _tasks.SaveChangesAsync();
        return true;
    }

    public async Task<TaskItemDto?> MoveTaskAsync(Guid id, MoveTaskRequest request, Guid userId)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task is null) throw new DomainError(DomainErrorKind.NotFound, "Task not found.");
        if (!await TaskVisibleAsync(task, userId)) throw new DomainError(DomainErrorKind.Forbidden, "Not a member of the containing workspace.");
        var target = await _columns.GetByIdAsync(request.ColumnId);
        if (target is null) throw new DomainError(DomainErrorKind.Validation, "Target column not found.");
        var column = await _columns.GetByIdAsync(task.ColumnId);
        var board = await _boards.GetByIdAsync(column!.BoardId);
        var targetColumn = await _columns.GetByIdAsync(request.ColumnId);
        if (targetColumn!.BoardId != board!.Id)
            throw new DomainError(DomainErrorKind.Validation, "Target column must belong to the same board.");
        task.ColumnId = request.ColumnId;
        task.BoardId = targetColumn!.BoardId;
        task.Position = (decimal)request.NewPosition;
        task.UpdatedAt = DateTime.UtcNow;
        _tasks.Update(task);
        await _tasks.SaveChangesAsync();
        return ToTaskDto(task);
    }

    public async Task<BulkUpdateTaskStatusResponse> BulkUpdateTaskStatusAsync(BulkUpdateTaskStatusRequest request, Guid userId)
    {
        // Validate workspace membership
        var ws = await _workspaces.GetByIdAsync(request.WorkspaceId);
        if (ws is null) throw new DomainError(DomainErrorKind.NotFound, "Workspace not found.");
        if (!IsMember(ws, userId)) throw new DomainError(DomainErrorKind.Forbidden, "Not a member of this workspace.");
        if (request.TaskIds is null || request.TaskIds.Count == 0)
            throw new DomainError(DomainErrorKind.Validation, "No task IDs provided.");

        // Validate status value
        if (!Enum.TryParse<Griot.Domain.Enums.TaskStatus>(request.Status, true, out var newStatus))
            throw new DomainError(DomainErrorKind.Validation, $"Invalid status value: '{request.Status}'.");

        // Collect workspace column IDs to verify task ownership
        var wsProjectIds = (await _projects.GetAllAsync()).Where(p => p.WorkspaceId == request.WorkspaceId).Select(p => p.Id).ToList();
        var wsBoardIds   = (await _boards.GetAllAsync()).Where(b => wsProjectIds.Contains(b.ProjectId)).Select(b => b.Id).ToList();
        var wsColumnIds  = (await _columns.GetAllAsync()).Where(c => wsBoardIds.Contains(c.BoardId)).Select(c => c.Id).ToList();

        var updated = 0;
        foreach (var tid in request.TaskIds)
        {
            var task = await _tasks.GetByIdAsync(tid);
            if (task is null || !wsColumnIds.Contains(task.ColumnId))
                throw new DomainError(DomainErrorKind.Conflict, $"Task {tid} does not belong to this workspace.");
            task.Status = newStatus;
            task.UpdatedAt = DateTime.UtcNow;
            _tasks.Update(task);
            updated++;
        }
        await _tasks.SaveChangesAsync();
        return new BulkUpdateTaskStatusResponse { Success = true, UpdatedCount = updated, Message = $"Updated {updated} task(s) to {newStatus}." };
    }

    // ══════════════════════ BOARD UPDATE / DELETE ══════════════════════

    public async Task<BoardDto?> UpdateBoardAsync(Guid id, UpdateBoardRequest request, Guid userId)
    {
        var board = await _boards.GetByIdAsync(id);
        if (board is null) throw new DomainError(DomainErrorKind.NotFound, "Board not found.");
        var project = await _projects.GetByIdAsync(board.ProjectId);
        await ScopedWorkspaceAsync(project!.WorkspaceId, userId);
        if (!string.IsNullOrWhiteSpace(request.Name)) board.Name = request.Name.Trim();
        if (request.Order is not null) board.Order = request.Order.Value;
        _boards.Update(board);
        await _boards.SaveChangesAsync();
        return ToBoardDto(board);
    }

    public async Task<bool> DeleteBoardAsync(Guid id, Guid userId)
    {
        var board = await _boards.GetByIdAsync(id);
        if (board is null) throw new DomainError(DomainErrorKind.NotFound, "Board not found.");
        var project = await _projects.GetByIdAsync(board.ProjectId);
        var ws = await ScopedWorkspaceAsync(project!.WorkspaceId, userId);
        if (!CanManageMembers(ws!, userId))
            throw new DomainError(DomainErrorKind.Forbidden, "Owner/Admin required to delete boards.");
        _boards.Remove(board);
        await _boards.SaveChangesAsync();
        return true;
    }

    // ══════════════════════ COMMENTS ══════════════════════

    public async Task<List<CommentDto>> GetCommentsAsync(Guid taskId, Guid userId)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        if (task is null || !await TaskVisibleAsync(task, userId)) throw new DomainError(DomainErrorKind.NotFound, "Task not found.");
        return (await _comments.GetAllAsync())
            .Where(c => c.TaskId == taskId)
            .Select(ToCommentDto).ToList();
    }

    public async Task<CommentDto?> CreateCommentAsync(Guid taskId, CreateCommentRequest request, Guid userId)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        if (task is null || !await TaskVisibleAsync(task, userId)) throw new DomainError(DomainErrorKind.NotFound, "Task not found.");
        RequireText(request.Body, "Body is required.");
        var comment = new Comment
        {
            Id = Guid.NewGuid(), TaskId = taskId, AuthorId = userId, Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        await _comments.AddAsync(comment);
        await _comments.SaveChangesAsync();
        return ToCommentDto(comment);
    }

    public async Task<CommentDto?> UpdateCommentAsync(Guid taskId, Guid commentId, UpdateCommentRequest request, Guid userId)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        if (task is null || !await TaskVisibleAsync(task, userId)) throw new DomainError(DomainErrorKind.NotFound, "Task not found.");
        var comment = (await _comments.GetAllAsync()).FirstOrDefault(c => c.Id == commentId && c.TaskId == taskId);
        if (comment is null) throw new DomainError(DomainErrorKind.NotFound, "Comment not found.");
        if (comment.AuthorId != userId) throw new DomainError(DomainErrorKind.Forbidden, "Only the author can edit this comment.");
        RequireText(request.Body, "Body is required.");
        comment.Body = request.Body.Trim();
        comment.UpdatedAt = DateTime.UtcNow;
        _comments.Update(comment);
        await _comments.SaveChangesAsync();
        return ToCommentDto(comment);
    }

    public async Task<bool> DeleteCommentAsync(Guid taskId, Guid commentId, Guid userId)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        if (task is null || !await TaskVisibleAsync(task, userId)) throw new DomainError(DomainErrorKind.NotFound, "Task not found.");
        var comment = (await _comments.GetAllAsync()).FirstOrDefault(c => c.Id == commentId && c.TaskId == taskId);
        if (comment is null) return false;
        if (comment.AuthorId != userId) throw new DomainError(DomainErrorKind.Forbidden, "Only the author can delete this comment.");
        _comments.Remove(comment);
        await _comments.SaveChangesAsync();
        return true;
    }

    // ══════════════════════ ATTACHMENTS ══════════════════════

    public async Task<List<AttachmentDto>> GetAttachmentsAsync(Guid taskId, Guid userId)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        if (task is null || !await TaskVisibleAsync(task, userId)) throw new DomainError(DomainErrorKind.NotFound, "Task not found.");
        return (await _attachments.GetAllAsync())
            .Where(a => a.TaskId == taskId)
            .Select(ToAttachmentDto).ToList();
    }

    public async Task<AttachmentDto?> CreateAttachmentAsync(Guid taskId, CreateAttachmentRequest request, Guid userId)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        if (task is null || !await TaskVisibleAsync(task, userId)) throw new DomainError(DomainErrorKind.NotFound, "Task not found.");
        RequireText(request.FileName, "FileName is required.");
        RequireText(request.MimeType, "MimeType is required.");
        var mimeType = request.MimeType.Trim();
        if (request.SizeBytes < 0 || request.SizeBytes > MaxAttachmentSizeBytes)
            throw new DomainError(DomainErrorKind.Validation, $"SizeBytes must be between 0 and {MaxAttachmentSizeBytes} bytes (25 MB).");
        if (!AllowedAttachmentMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
            throw new DomainError(DomainErrorKind.Validation, $"MimeType '{mimeType}' is not allowed. Allowed: images (jpeg/png/gif/webp), PDF, .docx, .xlsx.");
        var attachment = new Attachment
        {
            Id = Guid.NewGuid(), TaskId = taskId, UploaderId = userId,
            FileName = request.FileName.Trim(), MimeType = mimeType,
            SizeBytes = request.SizeBytes, StorageUrl = request.Url ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };
        await _attachments.AddAsync(attachment);
        await _attachments.SaveChangesAsync();
        return ToAttachmentDto(attachment);
    }

    public async Task<bool> DeleteAttachmentAsync(Guid taskId, Guid attachmentId, Guid userId)
    {
        var task = await _tasks.GetByIdAsync(taskId);
        if (task is null || !await TaskVisibleAsync(task, userId)) throw new DomainError(DomainErrorKind.NotFound, "Task not found.");
        var attachment = (await _attachments.GetAllAsync()).FirstOrDefault(a => a.Id == attachmentId && a.TaskId == taskId);
        if (attachment is null) return false;
        _attachments.Remove(attachment);
        await _attachments.SaveChangesAsync();
        return true;
    }

    // ══════════════════════ INVITES ══════════════════════

    public async Task<InviteDto?> GetInviteByTokenAsync(string token)
    {
        var invite = (await _invites.GetAllAsync()).FirstOrDefault(i => i.Token == token);
        return invite is null ? null : ToInviteDto(invite);
    }

    // ══════════════════════ NOTIFICATIONS ══════════════════════

    public async Task<List<NotificationDto>> GetNotificationsAsync(Guid userId)
        => (await _notifications.GetAllAsync())
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(ToNotificationDto).ToList();

    public async Task<int> GetUnreadNotificationCountAsync(Guid userId)
        => (await _notifications.GetAllAsync())
            .Count(n => n.UserId == userId && n.ReadAt is null);

    public async Task<bool> MarkAllNotificationsReadAsync(Guid userId)
    {
        foreach (var n in (await _notifications.GetAllAsync()).Where(n => n.UserId == userId && n.ReadAt is null))
        {
            n.ReadAt = DateTime.UtcNow;
            _notifications.Update(n);
        }
        await _notifications.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkNotificationReadAsync(Guid notificationId, Guid userId)
    {
        var n = await _notifications.GetByIdAsync(notificationId);
        if (n is null || n.UserId != userId) return false;
        if (n.ReadAt is not null) return true; // already read — idempotent
        n.ReadAt = DateTime.UtcNow;
        _notifications.Update(n);
        await _notifications.SaveChangesAsync();
        return true;
    }

    // ══════════════════════ DASHBOARD & LOGS ══════════════════════

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid workspaceId, Guid userId)
    {
        await ScopedWorkspaceAsync(workspaceId, userId);
        var counts = new Dictionary<string, int>();
        var urgentOpen = 0;
        foreach (var status in new[] { Griot.Domain.Enums.TaskStatus.Backlog, Griot.Domain.Enums.TaskStatus.Todo, Griot.Domain.Enums.TaskStatus.InProgress, Griot.Domain.Enums.TaskStatus.InReview, Griot.Domain.Enums.TaskStatus.Done })
            counts[status.ToString()] = 0;
        var wsProjectIds = (await _projects.GetAllAsync()).Where(p => p.WorkspaceId == workspaceId).Select(p => p.Id).ToList();
        var wsBoardIds = (await _boards.GetAllAsync()).Where(b => wsProjectIds.Contains(b.ProjectId)).Select(b => b.Id).ToList();
        var wsColumnIds = (await _columns.GetAllAsync()).Where(c => wsBoardIds.Contains(c.BoardId)).Select(c => c.Id).ToList();
        foreach (var t in (await _tasks.GetAllAsync()).Where(t => wsColumnIds.Contains(t.ColumnId)))
        {
            counts[t.Status.ToString()] = (counts.ContainsKey(t.Status.ToString()) ? counts[t.Status.ToString()] : 0) + 1;
            if (t.Status == Griot.Domain.Enums.TaskStatus.InProgress && t.Priority == Priority.Urgent) urgentOpen++;
        }
        var recentActivity = (await _activity.GetAllAsync())
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(ToActivityDto).ToList();
        return new DashboardSummaryDto
        {
            TaskCountsByStatus = counts, UrgentOpenCount = urgentOpen, RecentActivity = recentActivity
        };
    }

    public async Task<List<ActivityLogDto>> GetActivityAsync(Guid workspaceId, Guid userId, int page, int pageSize)
    {
        await ScopedWorkspaceAsync(workspaceId, userId);
        var pageNumber = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);
        return (await _activity.GetAllAsync())
            .Where(a => a.WorkspaceId == workspaceId)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .Select(ToActivityDto).ToList();
    }

    public async Task<List<ErrorLogDto>> GetErrorLogsAsync(Guid workspaceId, Guid userId, int limit)
    {
        await ScopedWorkspaceAsync(workspaceId, userId);
        var ws = await _workspaces.GetByIdAsync(workspaceId);
        if (ws is null || (ws.OwnerId != userId && !(await _members.GetAllAsync()).Any(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.Role == WorkspaceRole.Admin)))
            throw new DomainError(DomainErrorKind.Forbidden, "Owner/Admin required to view error logs.");
        return (await _errors.GetAllAsync())
            .OrderByDescending(e => e.CreatedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .Select(ToErrorLogDto).ToList();
    }

    public async Task<List<AuditLogDto>> GetAuditLogsAsync(Guid workspaceId, Guid userId, string? entityType, Guid? entityId, int limit)
    {
        await ScopedWorkspaceAsync(workspaceId, userId);
        var ws = await _workspaces.GetByIdAsync(workspaceId);
        if (ws is null || ws.OwnerId != userId)
            throw new DomainError(DomainErrorKind.Forbidden, "Owner-only to view audit logs.");
        var all = (await _audit.GetAllAsync()).ToList();
        if (!string.IsNullOrWhiteSpace(entityType)) all = all.Where(a => a.EntityType == entityType).ToList();
        if (entityId is not null) all = all.Where(a => a.EntityId == entityId).ToList();
        return all.OrderByDescending(a => a.CreatedAt).Take(Math.Clamp(limit, 1, 200)).Select(ToAuditLogDto).ToList();
    }

    // ══════════════════════ HELPERS ══════════════════════

    private async Task<Workspace?> ScopedWorkspaceAsync(Guid workspaceId, Guid userId)
    {
        var ws = await _workspaces.GetByIdAsync(workspaceId);
        if (ws is null) throw new DomainError(DomainErrorKind.NotFound, "Workspace not found.");
        if (!IsMember(ws, userId)) throw new DomainError(DomainErrorKind.Forbidden, "Not a member of this workspace.");
        return ws;
    }

    private bool IsMember(Workspace ws, Guid userId)
        => ws.OwnerId == userId || ws.Members.Any(m => m.UserId == userId);

    private bool CanManageMembers(Workspace ws, Guid userId)
        => ws.OwnerId == userId || ws.Members.Any(m => m.UserId == userId && m.Role == WorkspaceRole.Admin);

    private static void RequireText(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainError(DomainErrorKind.Validation, message);
    }

    private static string Slugify(string name)
    {
        var lower = name.ToLowerInvariant().Trim();
        var sb = new StringBuilder();
        for (var i = 0; i < lower.Length; i++)
        {
            var ch = lower[i];
            sb.Append(Char.IsLetterOrDigit(ch) ? ch : '-');
        }
        return sb.ToString().Trim('-');
    }

    // DTO mappers
    private WorkspaceDto ToWorkspaceDto(Workspace w)
    {
        var dto = new WorkspaceDto { Id = w.Id, Name = w.Name, Slug = w.Slug, OwnerId = w.OwnerId, CreatedAt = w.CreatedAt, UpdatedAt = w.UpdatedAt };
        dto.Members = w.Members?.Select(ToMemberDto).ToList() ?? new();
        return dto;
    }
    private WorkspaceMemberDto ToMemberDto(WorkspaceMember m) => new()
    {
        UserId = m.UserId, DisplayName = m.User?.DisplayName, Email = m.User?.Email,
        AvatarUrl = m.User?.AvatarUrl, Role = m.Role, JoinedAt = m.JoinedAt
    };
    private InviteDto ToInviteDto(Invite i) => new()
    {
        Id = i.Id, WorkspaceId = i.WorkspaceId, Email = i.Email, Role = i.Role,
        Token = i.Token, ExpiresAt = i.ExpiresAt
    };
    private ProjectDto ToProjectDto(Project p) => new()
    {
        Id = p.Id, WorkspaceId = p.WorkspaceId, Name = p.Name, Key = p.Key, Description = p.Description,
        Status = p.Status, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt
    };
    private BoardDto ToBoardDto(Board b)
    {
        var dto = new BoardDto { Id = b.Id, ProjectId = b.ProjectId, Name = b.Name, Order = b.Order, CreatedAt = b.CreatedAt };
        dto.Columns = b.Columns?.Select(ToColumnDto).ToList() ?? new();
        return dto;
    }
    private ColumnDto ToColumnDto(Column c)
    {
        var dto = new ColumnDto { Id = c.Id, BoardId = c.BoardId, Name = c.Name, Order = c.Order, WipLimit = c.WipLimit, CreatedAt = c.CreatedAt };
        dto.TaskItems = c.TaskItems?.Select(ToTaskDto).ToList() ?? new();
        return dto;
    }
    private TaskItemDto ToTaskDto(TaskItem t) => new()
    {
        Id = t.Id, BoardId = t.BoardId, ColumnId = t.ColumnId, Title = t.Title, Description = t.Description, Status = t.Status,
        Priority = t.Priority, AssigneeId = t.AssigneeId, CreatorId = t.CreatorId, DueDate = t.DueDate,
        Position = t.Position, CreatedAt = t.CreatedAt, UpdatedAt = t.UpdatedAt
    };
    private CommentDto ToCommentDto(Comment c) => new()
    {
        Id = c.Id, TaskId = c.TaskId, AuthorId = c.AuthorId, AuthorName = c.Author?.DisplayName,
        AuthorAvatarUrl = c.Author?.AvatarUrl, Body = c.Body, CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt
    };
    private AttachmentDto ToAttachmentDto(Attachment a) => new()
    {
        Id = a.Id, TaskId = a.TaskId, UploaderId = a.UploaderId, UploaderName = a.Uploader?.DisplayName,
        FileName = a.FileName, MimeType = a.MimeType, SizeBytes = a.SizeBytes,
        Url = string.IsNullOrEmpty(a.StorageUrl) ? null : a.StorageUrl,
        CreatedAt = a.CreatedAt
    };
    private NotificationDto ToNotificationDto(Notification n) => new()
    {
        Id = n.Id, Type = n.Type, Title = n.Title, Body = n.Body, TargetRef = n.TargetRef,
        ReadAt = n.ReadAt, CreatedAt = n.CreatedAt
    };
    private ActivityLogDto ToActivityDto(ActivityLog a) => new()
    {
        Id = a.Id, EntityType = a.EntityType, EntityId = a.EntityId, Action = a.Action,
        Payload = a.Payload, CreatedAt = a.CreatedAt, ActorName = a.Actor?.DisplayName, ActorAvatarUrl = a.Actor?.AvatarUrl
    };
    private ErrorLogDto ToErrorLogDto(ErrorLog e) => new()
    {
        Id = e.Id, RequestId = e.RequestId, ExceptionType = e.ExceptionType, Message = e.Message,
        FixStatus = e.FixStatus, CreatedAt = e.CreatedAt
    };
    private AuditLogDto ToAuditLogDto(AuditLog a) => new()
    {
        Id = a.Id, ActorId = a.ActorId, Action = a.Action, EntityType = a.EntityType, EntityId = a.EntityId,
        Before = a.Before, After = a.After, CreatedAt = a.CreatedAt
    };

    private static int ComputeMaxInt(List<int> values)
    {
        if (values == null || values.Count == 0) return 0;
        var max = values[0];
        for (var i = 1; i < values.Count; i++)
            if (values[i] > max) max = values[i];
        return max;
    }
}
