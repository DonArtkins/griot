using System.Collections.Generic;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;

namespace Griot.Application.Services;

public class WorkspaceService : IWorkspaceService
{
    public async Task<List<WorkspaceDto>> GetWorkspacesAsync(Guid userId) => new();

    public async Task<WorkspaceDto> CreateWorkspaceAsync(CreateWorkspaceRequest request, Guid ownerId)
        => null!;
}
