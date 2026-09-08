using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;
using Griot.Application.Interfaces.Repositories;
using Griot.Domain.Enums;

namespace Griot.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;

    public TaskService(ITaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<BulkUpdateTaskStatusResponse> BulkUpdateStatusAsync(BulkUpdateTaskStatusRequest request, Guid userId)
    {
        // Pre-validation: empty array or malformed body
        if (request.TaskIds == null || !request.TaskIds.Any())
        {
            return new BulkUpdateTaskStatusResponse
            {
                Success = false,
                UpdatedCount = 0,
                Message = "Task IDs array cannot be empty"
            };
        }

        // Deduplicate task IDs (handles duplicate IDs in request)
        var distinctTaskIds = request.TaskIds.Distinct().ToList();

        // Validate status enum
        if (!Enum.TryParse<Griot.Domain.Enums.TaskStatus>(request.Status, true, out var statusEnum))
        {
            return new BulkUpdateTaskStatusResponse
            {
                Success = false,
                UpdatedCount = 0,
                Message = $"Invalid status value: {request.Status}"
            };
        }

        // Check workspace membership (Owner/Admin/Member)
        var isMember = await _taskRepository.IsUserWorkspaceMemberAsync(request.WorkspaceId, userId);
        if (!isMember)
        {
            return new BulkUpdateTaskStatusResponse
            {
                Success = false,
                UpdatedCount = 0,
                Message = "User is not a member of the specified workspace"
            };
        }

        // Validate all task IDs belong to the workspace (uses deduplicated list)
        var allTasksValid = await _taskRepository.ValidateTasksInWorkspaceAsync(request.WorkspaceId, distinctTaskIds);
        if (!allTasksValid)
        {
            // Return 409 for any invalid task id in batch (per api-surface.md)
            return new BulkUpdateTaskStatusResponse
            {
                Success = false,
                UpdatedCount = 0,
                Message = $"One or more task IDs are invalid or do not belong to workspace {request.WorkspaceId}"
            };
        }

        // All validations passed - execute bulk update via stored procedure (uses deduplicated list)
        try
        {
            await _taskRepository.BulkUpdateStatusAsync(
                request.WorkspaceId,
                distinctTaskIds,
                request.Status
            );

            return new BulkUpdateTaskStatusResponse
            {
                Success = true,
                UpdatedCount = distinctTaskIds.Count,
                Message = $"Successfully updated {distinctTaskIds.Count} task(s)"
            };
        }
        catch (Exception)
        {
            // Stored procedure failed (transaction rolled back)
            // Don't expose internal details - return generic error
            return new BulkUpdateTaskStatusResponse
            {
                Success = false,
                UpdatedCount = 0,
                Message = "Bulk update failed due to a server error"
            };
        }
    }
}
