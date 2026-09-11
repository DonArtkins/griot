using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Griot.Application.DTOs;

namespace Griot.Application.Interfaces.Services;

/// <summary>
/// Domain operations for the implemented REST surface (specs 13–17).
/// Every method is scoped to the authenticated user: null return = not found /
/// not visible, and errors are signalled via <see cref="DomainError"/> (mapped by
/// controllers to 400/403/404/409).
/// </summary>
public interface IDomainService
{
    // ── Workspaces ────────────────────────────────────────────────────────────
    Task<List<WorkspaceDto>> GetWorkspacesAsync(Guid userId);
    Task<WorkspaceDto> CreateWorkspaceAsync(CreateWorkspaceRequest request, Guid ownerId);
    Task<WorkspaceDto?> GetWorkspaceAsync(Guid id, Guid userId);
    Task<WorkspaceDto?> UpdateWorkspaceAsync(Guid id, UpdateWorkspaceRequest request, Guid userId);
    Task<bool> DeleteWorkspaceAsync(Guid id, Guid userId);
    Task<List<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, Guid userId);
    Task<WorkspaceMemberDto?> AddMemberAsync(Guid workspaceId, AddMemberRequest request, Guid userId);
    Task<WorkspaceMemberDto?> UpdateMemberRoleAsync(Guid workspaceId, Guid memberUserId, UpdateMemberRoleRequest request, Guid userId);
    Task<bool> RemoveMemberAsync(Guid workspaceId, Guid memberUserId, Guid userId);
    Task<InviteDto?> CreateInviteAsync(Guid workspaceId, CreateInviteRequest request, Guid userId);
    Task<bool> AcceptInviteAsync(string token, Guid userId);
    Task<bool> IsUserMemberAsync(Guid workspaceId, Guid userId);

    // ── Projects ──────────────────────────────────────────────────────────────
    Task<List<ProjectDto>> GetProjectsAsync(Guid workspaceId, Guid userId);
    Task<ProjectDto> CreateProjectAsync(Guid workspaceId, CreateProjectRequest request, Guid userId);
    Task<ProjectDto?> GetProjectAsync(Guid id, Guid userId);
    Task<ProjectDto?> UpdateProjectAsync(Guid id, UpdateProjectRequest request, Guid userId);
    Task<bool> DeleteProjectAsync(Guid id, Guid userId);

    // ── Boards & Columns ──────────────────────────────────────────────────────
    Task<List<BoardDto>> GetBoardsAsync(Guid projectId, Guid userId);
    Task<BoardDto> CreateBoardAsync(Guid projectId, CreateBoardRequest request, Guid userId);
    Task<BoardDto?> GetBoardAsync(Guid id, Guid userId);
    Task<BoardDto?> UpdateBoardAsync(Guid id, UpdateBoardRequest request, Guid userId);
    Task<bool> DeleteBoardAsync(Guid id, Guid userId);
    Task<ColumnDto?> CreateColumnAsync(Guid boardId, CreateColumnRequest request, Guid userId);
    Task<ColumnDto?> UpdateColumnAsync(Guid id, UpdateColumnRequest request, Guid userId);
    Task<bool> DeleteColumnAsync(Guid id, Guid userId);

    // ── Tasks ─────────────────────────────────────────────────────────────────
    Task<List<TaskItemDto>> GetTasksAsync(Guid boardId, Guid userId);
    Task<TaskItemDto?> CreateTaskAsync(Guid boardId, CreateTaskRequest request, Guid userId);
    Task<TaskItemDto?> GetTaskAsync(Guid id, Guid userId);
    Task<TaskItemDto?> UpdateTaskAsync(Guid id, UpdateTaskRequest request, Guid userId);
    Task<bool> DeleteTaskAsync(Guid id, Guid userId);
    Task<TaskItemDto?> MoveTaskAsync(Guid id, MoveTaskRequest request, Guid userId);
    Task<BulkUpdateTaskStatusResponse> BulkUpdateTaskStatusAsync(BulkUpdateTaskStatusRequest request, Guid userId);

    // ── Comments ──────────────────────────────────────────────────────────────
    Task<List<CommentDto>> GetCommentsAsync(Guid taskId, Guid userId);
    Task<CommentDto?> CreateCommentAsync(Guid taskId, CreateCommentRequest request, Guid userId);
    Task<CommentDto?> UpdateCommentAsync(Guid taskId, Guid commentId, UpdateCommentRequest request, Guid userId);
    Task<bool> DeleteCommentAsync(Guid taskId, Guid commentId, Guid userId);

    // ── Attachments (metadata only — spec 17) ─────────────────────────────────
    Task<List<AttachmentDto>> GetAttachmentsAsync(Guid taskId, Guid userId);
    Task<AttachmentDto?> CreateAttachmentAsync(Guid taskId, CreateAttachmentRequest request, Guid userId);
    Task<bool> DeleteAttachmentAsync(Guid taskId, Guid attachmentId, Guid userId);

    // ── Invites ───────────────────────────────────────────────────────────────
    Task<InviteDto?> GetInviteByTokenAsync(string token);

    // ── Notifications ─────────────────────────────────────────────────────────
    Task<List<NotificationDto>> GetNotificationsAsync(Guid userId);
    Task<int> GetUnreadNotificationCountAsync(Guid userId);
    Task<bool> MarkAllNotificationsReadAsync(Guid userId);
    Task<bool> MarkNotificationReadAsync(Guid notificationId, Guid userId);

    // ── Dashboard & Logs ──────────────────────────────────────────────────────
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(Guid workspaceId, Guid userId);
    Task<List<ActivityLogDto>> GetActivityAsync(Guid workspaceId, Guid userId, int page, int pageSize);
    Task<List<ErrorLogDto>> GetErrorLogsAsync(Guid workspaceId, Guid userId, int limit);

    /// <summary>
    /// Spec 20: FixStatus lifecycle for one ErrorLog (Owner/Admin of the workspace).
    /// Setting Fixed/Verified/WontFix stamps FixedAt + SolvedByUserId (the retention
    /// procedure only prunes resolved errors); Open/Investigating clears them.
    /// </summary>
    Task<ErrorLogDto> UpdateErrorLogStatusAsync(Guid workspaceId, Guid errorId, Guid userId, Domain.Enums.ErrorFixStatus fixStatus);
    Task<List<AuditLogDto>> GetAuditLogsAsync(Guid workspaceId, Guid userId, string? entityType, Guid? entityId, int limit);
}