using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Griot.Application.DTOs;

namespace Griot.Application.Interfaces.Services;

public interface IWorkspaceService
{
    Task<List<WorkspaceDto>> GetWorkspacesAsync(Guid userId);
    Task<WorkspaceDto> CreateWorkspaceAsync(CreateWorkspaceRequest request, Guid ownerId);
}
