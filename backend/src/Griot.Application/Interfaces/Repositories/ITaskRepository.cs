using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Griot.Application.Interfaces.Repositories;

public interface ITaskRepository
{
    Task BulkUpdateStatusAsync(Guid workspaceId, IEnumerable<Guid> taskIds, string status);
    Task<bool> IsUserWorkspaceMemberAsync(Guid workspaceId, Guid userId);
    Task<bool> ValidateTasksInWorkspaceAsync(Guid workspaceId, IEnumerable<Guid> taskIds);
}
