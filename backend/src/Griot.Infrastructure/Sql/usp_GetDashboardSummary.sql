CREATE OR ALTER PROCEDURE dbo.usp_GetDashboardSummary
    @WorkspaceId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    -- Task counts by status
    SELECT 
        t.[Status], 
        COUNT(*) AS [Count]
    FROM dbo.TaskItems t
    INNER JOIN dbo.Columns c ON t.ColumnId = c.Id
    INNER JOIN dbo.Boards b ON c.BoardId = b.Id
    INNER JOIN dbo.Projects p ON b.ProjectId = p.Id
    WHERE p.WorkspaceId = @WorkspaceId
    GROUP BY t.[Status];

    -- Urgent open tasks
    SELECT 
        COUNT(*) AS UrgentOpenCount
    FROM dbo.TaskItems t
    INNER JOIN dbo.Columns c ON t.ColumnId = c.Id
    INNER JOIN dbo.Boards b ON c.BoardId = b.Id
    INNER JOIN dbo.Projects p ON b.ProjectId = p.Id
    WHERE p.WorkspaceId = @WorkspaceId
      AND t.Priority = 'Urgent'
      AND t.[Status] NOT IN ('Done');

    -- Recent activity head (latest 10)
    SELECT TOP 10
        a.Id,
        a.EntityType,
        a.EntityId,
        a.Action,
        a.Payload,
        a.CreatedAt,
        u.DisplayName AS ActorName,
        u.AvatarUrl AS ActorAvatarUrl
    FROM dbo.ActivityLogs a
    LEFT JOIN dbo.Users u ON a.ActorId = u.Id
    WHERE a.WorkspaceId = @WorkspaceId
    ORDER BY a.CreatedAt DESC;
END;
GO
