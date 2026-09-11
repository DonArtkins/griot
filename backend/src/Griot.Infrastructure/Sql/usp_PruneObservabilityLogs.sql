-- ============================================================================
-- usp_PruneObservabilityLogs (spec 20, pipeline §5 — retention)
-- Idempotent: safe to re-run (release step / CI-of-records / scheduled job).
--   ApiLogs      → deleted after 90 days (default) by CreatedAt
--   ErrorLogs    → deleted after 90 days (default) ONLY once FixedAt is set
--                  (unfixed errors survive the retention tail until triaged —
--                  the FixStatus lifecycle is the mitigation path)
--   AuditLogs    → deleted after 365 days (default) — compliance tail
--   ActivityLogs → deleted after 180 days (default)
-- Retention windows are parameters so environments can tighten/loosen without
-- editing the procedure. Returns the per-table deleted-row counts.
-- ============================================================================

CREATE OR ALTER PROCEDURE dbo.usp_PruneObservabilityLogs
    @ApiRetentionDays      INT = 90,
    @ErrorRetentionDays    INT = 90,
    @AuditRetentionDays    INT = 365,
    @ActivityRetentionDays INT = 180
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @ApiDeleted      INT = 0,
        @ErrorDeleted    INT = 0,
        @AuditDeleted    INT = 0,
        @ActivityDeleted INT = 0;

    BEGIN TRAN;

        DELETE FROM dbo.ApiLogs
        WHERE CreatedAt < DATEADD(DAY, -@ApiRetentionDays, SYSUTCDATETIME());
        SET @ApiDeleted = @@ROWCOUNT;

        -- Only RESOLVED errors age out; Open/Investigating rows are kept no matter
        -- how old so the incident record is never pruned before triage.
        DELETE FROM dbo.ErrorLogs
        WHERE FixedAt IS NOT NULL
          AND FixedAt  < DATEADD(DAY, -@ErrorRetentionDays, SYSUTCDATETIME());
        SET @ErrorDeleted = @@ROWCOUNT;

        DELETE FROM dbo.AuditLogs
        WHERE CreatedAt < DATEADD(DAY, -@AuditRetentionDays, SYSUTCDATETIME());
        SET @AuditDeleted = @@ROWCOUNT;

        DELETE FROM dbo.ActivityLogs
        WHERE CreatedAt < DATEADD(DAY, -@ActivityRetentionDays, SYSUTCDATETIME());
        SET @ActivityDeleted = @@ROWCOUNT;

    COMMIT;

    SELECT
        @ApiDeleted      AS ApiLogsDeleted,
        @ErrorDeleted    AS ErrorLogsDeleted,
        @AuditDeleted    AS AuditLogsDeleted,
        @ActivityDeleted AS ActivityLogsDeleted;
END
GO
