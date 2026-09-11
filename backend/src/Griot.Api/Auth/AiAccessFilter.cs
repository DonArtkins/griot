using Griot.Api.Controllers;
using Griot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Griot.Api.Auth;

/// <summary>Default-deny AI authorization runs before model binding and domain actions.</summary>
public sealed class AiAccessFilter : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        if (!AiAccess.IsAi(http.User)) return;
        var action = context.ActionDescriptor as ControllerActionDescriptor;
        var (scope, resource, argument) = (action?.ControllerTypeInfo.AsType(), action?.ActionName) switch
        {
            (var t, nameof(WorkspaceController.GetWorkspaces)) when t == typeof(WorkspaceController)
                => (ServiceTokenHandler.ScopeReadWorkspace, "list", ""),
            (var t, nameof(WorkspaceController.GetWorkspace) or nameof(WorkspaceController.GetMembers)) when t == typeof(WorkspaceController)
                => (ServiceTokenHandler.ScopeReadWorkspace, "workspace", "id"),
            (var t, nameof(ProjectController.GetProjects)) when t == typeof(ProjectController)
                => (ServiceTokenHandler.ScopeReadWorkspace, "workspace", "id"),
            (var t, nameof(ProjectController.GetProject)) when t == typeof(ProjectController)
                => (ServiceTokenHandler.ScopeReadWorkspace, "project", "id"),
            (var t, nameof(BoardController.GetBoards)) when t == typeof(BoardController)
                => (ServiceTokenHandler.ScopeReadWorkspace, "project", "projectId"),
            (var t, nameof(BoardController.GetBoard)) when t == typeof(BoardController)
                => (ServiceTokenHandler.ScopeReadWorkspace, "board", "id"),
            (var t, nameof(TaskController.GetTasks)) when t == typeof(TaskController)
                => (ServiceTokenHandler.ScopeReadWorkspace, "board", "id"),
            (var t, nameof(TaskController.CreateTask)) when t == typeof(TaskController)
                => (ServiceTokenHandler.ScopeCreateTask, "board", "id"),
            (var t, nameof(TaskController.GetTask)) when t == typeof(TaskController)
                => (ServiceTokenHandler.ScopeReadWorkspace, "task", "id"),
            (var t, nameof(CommentController.GetComments)) when t == typeof(CommentController)
                => (ServiceTokenHandler.ScopeReadWorkspace, "task", "taskId"),
            (var t, nameof(CommentController.CreateComment)) when t == typeof(CommentController)
                => (ServiceTokenHandler.ScopeAddComment, "task", "taskId"),
            (var t, nameof(AttachmentController.GetAttachments)) when t == typeof(AttachmentController)
                => (ServiceTokenHandler.ScopeReadWorkspace, "task", "id"),
            (var t, nameof(DashboardController.GetSummary)) when t == typeof(DashboardController)
                => (ServiceTokenHandler.ScopeReadWorkspace, "workspace", "workspaceId"),
            (var t, nameof(DashboardController.GetActivity)) when t == typeof(DashboardController)
                => (ServiceTokenHandler.ScopeReadWorkspace, "workspace", "id"),
            _ => ((string?)null, "", "")
        };
        if (scope is null || !AiAccess.AllowsScope(http.User, scope))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }
        if (resource == "list") return; // GetWorkspaces filters the returned DTOs by grant.
        var value = http.Request.RouteValues[argument]?.ToString() ?? http.Request.Query[argument].ToString();
        if (!Guid.TryParse(value, out var id))
        {
            context.Result = new BadRequestResult();
            return;
        }
        var workspaceId = id;
        if (resource != "workspace")
        {
            var db = http.RequestServices.GetRequiredService<GriotDbContext>();
            var query = resource switch
            {
                "project" => db.Projects.Where(p => p.Id == id).Select(p => p.WorkspaceId),
                "board" => db.Boards.Where(b => b.Id == id).Select(b => b.Project.WorkspaceId),
                _ => db.TaskItems.Where(t => t.Id == id).Select(t => t.Column.Board.Project.WorkspaceId)
            };
            workspaceId = await query.FirstOrDefaultAsync(http.RequestAborted);
        }
        if (!AiAccess.AllowsWorkspace(http.User, workspaceId))
            context.Result = new NotFoundResult();
        // DomainService independently checks current ownership/membership before returning data.
    }
}
