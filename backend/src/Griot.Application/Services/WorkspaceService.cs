using System.Collections.Generic;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Services;

namespace Griot.Application.Services;

/// <summary>
/// Placeholder workspace service — kept only so <see cref="IWorkspaceService"/> resolves in DI.
/// All REST CRUD (specs 13–17) is served by <see cref="DomainService"/> via
/// <see cref="Griot.Api.Controllers.WorkspaceController"/>; this scaffold has no callers.
/// </summary>
public class WorkspaceService : IWorkspaceService
{
    public Task<List<WorkspaceDto>> GetWorkspacesAsync(Guid userId) => Task.FromResult(new List<WorkspaceDto>());

    public Task<WorkspaceDto> CreateWorkspaceAsync(CreateWorkspaceRequest request, Guid ownerId)
        => Task.FromResult<WorkspaceDto>(null!);
}
