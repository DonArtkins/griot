using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Griot.Application.Interfaces.Repositories;
using Griot.Infrastructure.Persistence;

namespace Griot.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly GriotDbContext _context;

    public TaskRepository(GriotDbContext context)
    {
        _context = context;
    }

    public async Task BulkUpdateStatusAsync(Guid workspaceId, IEnumerable<Guid> taskIds, string status)
    {
        var tvp = new DataTable();
        tvp.Columns.Add("Id", typeof(Guid));
        foreach (var id in taskIds)
        {
            tvp.Rows.Add(id);
        }

        var connection = _context.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed) await connection.OpenAsync();
        try
        {
            await connection.ExecuteAsync(
                "dbo.usp_BulkUpdateTaskStatus",
                new { WorkspaceId = workspaceId, TaskIds = tvp.AsTableValuedParameter("dbo.IdList"), Status = status },
                commandType: CommandType.StoredProcedure);
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }

    public async Task<bool> IsUserWorkspaceMemberAsync(Guid workspaceId, Guid userId)
    {
        // Check if user is member or owner
        var isMember = await _context.WorkspaceMembers
            .AnyAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId);

        if (isMember) return true;

        var isOwner = await _context.Workspaces
            .AnyAsync(w => w.Id == workspaceId && w.OwnerId == userId);

        return isOwner;
    }

    public async Task<bool> ValidateTasksInWorkspaceAsync(Guid workspaceId, IEnumerable<Guid> taskIds)
    {
        var taskIdList = taskIds.ToList();
        if (!taskIdList.Any()) return false;

        // Get all tasks in the workspace via Column -> Board -> Project -> Workspace hierarchy
        var taskIdsInWorkspace = await _context.TaskItems
            .Where(t => taskIdList.Contains(t.Id))
            .Include(t => t.Column)
                .ThenInclude(c => c.Board)
                    .ThenInclude(b => b.Project)
            .Where(t => t.Column.Board.Project.WorkspaceId == workspaceId)
            .Select(t => t.Id)
            .ToListAsync();

        // All provided task IDs must belong to the workspace
        return taskIdsInWorkspace.Count == taskIdList.Count;
    }
}
