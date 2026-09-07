using System;
using System.Threading.Tasks;
using Griot.Application.DTOs;

namespace Griot.Application.Interfaces.Repositories;

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid workspaceId);
}
