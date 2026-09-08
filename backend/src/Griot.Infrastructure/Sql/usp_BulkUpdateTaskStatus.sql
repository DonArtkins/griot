IF TYPE_ID('dbo.IdList') IS NULL
BEGIN
    CREATE TYPE dbo.IdList AS TABLE(
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY
    );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_BulkUpdateTaskStatus
    @WorkspaceId UNIQUEIDENTIFIER,
    @TaskIds     dbo.IdList READONLY,
    @Status      NVARCHAR(32)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    DECLARE @RequestedCount INT;
    DECLARE @AffectedCount INT;
    
    -- Count how many IDs were requested
    SELECT @RequestedCount = COUNT(*) FROM @TaskIds;
    
    BEGIN TRAN;
        UPDATE t SET t.[Status] = @Status, t.UpdatedAt = SYSUTCDATETIME()
        FROM dbo.TaskItems t
        INNER JOIN @TaskIds ids ON ids.Id = t.Id
        INNER JOIN dbo.Columns c ON t.ColumnId = c.Id
        INNER JOIN dbo.Boards b ON c.BoardId = b.Id
        INNER JOIN dbo.Projects p ON b.ProjectId = p.Id
        WHERE p.WorkspaceId = @WorkspaceId;
        
        SET @AffectedCount = @@ROWCOUNT;
        
        -- Enforce all-or-nothing: if any task was not updated, roll back
        IF @AffectedCount <> @RequestedCount
        BEGIN
            ROLLBACK;
            THROW 50001, 'Bulk update failed: not all requested tasks were updated (partial update prevented)', 1;
        END;
    COMMIT;
END;
GO
