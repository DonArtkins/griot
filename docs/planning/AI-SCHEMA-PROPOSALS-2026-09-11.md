# AI schema proposals — 2026-09-11

Status: PLANNED, unapproved. This is a review input for Figma Make, not an approved ERD or permission to migrate. The previously referenced `diagrams/erd/ai-planning-amendments.md` did not exist. Approved exports remain in `diagrams/erd/`; each owning feature must complete its design and obtain approval before schema implementation.

## Backend 24: Report creation timestamp

Add immutable UTC `CreatedAt` to the existing Report alongside Title, CreatorUserId, Status and DeletedAt. `AddReportArtifacts` backfills CreatedAt from GeneratedAt once, requires it for new rows, and prevents subsequent application/job updates from changing it. `(CreatedAt, Id)` orders report cursors. GeneratedAt remains generation provenance. This is a proposal; the current entity has no CreatedAt.

## Backend 28: append-only ProjectEvidence

Proposed fields: Id, WorkspaceId, ProjectId, Kind, RecordedByUserId, RecordedAt, SupersedesId?, RunId?, Page?, TotalPages?, TotalResults?, ContentJson. Existing project/workspace/user relationships must be scoped and validated. No existing project needs a backfill.

For test_run, RunId is a required UUID, Page starts at 1, TotalPages is positive and TotalResults is the nonnegative declared number of results for the whole run. Page cannot exceed TotalPages. Other evidence kinds require these columns to be null. A filtered unique index on `(WorkspaceId, ProjectId, RunId, Page)` rejects duplicate pages with 409. Keep `(WorkspaceId, ProjectId, Kind, RecordedAt, Id)` for reads.

Insertion serializes validation for the run: totals, tester, execution timestamp, environment, deployment and baseline cannot differ across pages. In one consistent snapshot, all pages 1..TotalPages, exactly TotalResults entries and unique case IDs are required for report eligibility. No JSON grouping or client completion flag can substitute. Corrections append a full replacement under a new RunId with SupersedesId provenance. An incomplete replacement invalidates eligibility; original records remain auditable.

## Other pending amendments

Backend 20 owns run-ID indexes and durable job/outbox/inbox design; backend 25 owns the proposed User.SystemRole tier; backend 26 owns proposed conversation/preference/lesson records; backend 27 owns draft/audience and delivery-state persistence. Their owning specs define requirements. Their full fields, indexes and relationships still need Figma Make design review. This document does not approve those names or claim the system design is complete.
