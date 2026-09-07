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
    BEGIN TRAN;
        UPDATE t SET t.[Status] = @Status, t.UpdatedAt = SYSUTCDATETIME()
        FROM dbo.TaskItems t
        INNER JOIN @TaskIds ids ON ids.Id = t.Id
        INNER JOIN dbo.Columns c ON t.ColumnId = c.Id
        INNER JOIN dbo.Boards b ON c.BoardId = b.Id
        INNER JOIN dbo.Projects p ON b.ProjectId = p.Id
        WHERE p.WorkspaceId = @WorkspaceId;
    COMMIT;
END;
GO
