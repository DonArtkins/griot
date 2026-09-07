using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Griot.Application.DTOs;
using Griot.Application.Interfaces.Repositories;
using Griot.Infrastructure.Persistence;

namespace Griot.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly GriotDbContext _context;

    public DashboardRepository(GriotDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid workspaceId)
    {
        var connection = _context.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed) await connection.OpenAsync();
        
        try
        {
            using var multi = await connection.QueryMultipleAsync(
                "dbo.usp_GetDashboardSummary",
                new { WorkspaceId = workspaceId },
                commandType: CommandType.StoredProcedure);

            var taskCountsRaw = await multi.ReadAsync<TaskCountRow>();
            var taskCounts = taskCountsRaw.ToDictionary(x => x.Status, x => x.Count);

            var urgentOpenCount = await multi.ReadSingleAsync<int>();
            
            var recentActivity = (await multi.ReadAsync<ActivityLogDto>()).ToList();

            return new DashboardSummaryDto
            {
                TaskCountsByStatus = taskCounts,
                UrgentOpenCount = urgentOpenCount,
                RecentActivity = recentActivity
            };
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }

    private class TaskCountRow
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
