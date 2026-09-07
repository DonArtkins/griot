using System;
using System.Collections.Generic;
using System.Data;
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
}
