-- Seed data for GraphQL testing (local dev only)
-- Run: docker exec -i infra-sababisha-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'SababishaDev2026!' -C -d Griot < backend/src/Griot.Infrastructure/Sql/seed-test-data.sql

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- Clear existing data (for re-runs)
DELETE FROM Comments;
DELETE FROM Attachments;
DELETE FROM TaskItems;
DELETE FROM Columns;
DELETE FROM Boards;
DELETE FROM Projects;
DELETE FROM WorkspaceMembers;
DELETE FROM Workspaces;
DELETE FROM ActivityLogs;
DELETE FROM ApiLogs;
DELETE FROM AuditLogs;
DELETE FROM ErrorLogs;
DELETE FROM Notifications;
DELETE FROM OtpChallenges;
DELETE FROM RefreshTokens;
DELETE FROM Invites;
DELETE FROM Reports;
DELETE FROM Users;

-- Test users (passwords are hashed with Argon2 for 'Password123!')
-- Note: In production, passwords should be set via the /api/auth/register endpoint
-- Schema: Id, Email, DisplayName, AvatarUrl, PasswordHash, CreatedAt, UpdatedAt, TwoFactorMethod
INSERT INTO Users (Id, Email, DisplayName, PasswordHash, CreatedAt, UpdatedAt, TwoFactorMethod)
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'owner@griot.test', 'Owner User', '$argon2id$v=19$m=65536,t=3,p=1$randomsalt1234567890$hashedpasswordhere', GETUTCDATE(), GETUTCDATE(), 'None'),
    ('22222222-2222-2222-2222-222222222222', 'admin@griot.test', 'Admin User', '$argon2id$v=19$m=65536,t=3,p=1$randomsalt1234567890$hashedpasswordhere', GETUTCDATE(), GETUTCDATE(), 'None'),
    ('33333333-3333-3333-3333-333333333333', 'member@griot.test', 'Member User', '$argon2id$v=19$m=65536,t=3,p=1$randomsalt1234567890$hashedpasswordhere', GETUTCDATE(), GETUTCDATE(), 'None'),
    ('44444444-4444-4444-4444-444444444444', 'guest@griot.test', 'Guest User', '$argon2id$v=19$m=65536,t=3,p=1$randomsalt1234567890$hashedpasswordhere', GETUTCDATE(), GETUTCDATE(), 'None');

-- Test workspaces
-- Schema: Id, Name, Slug, OwnerId, CreatedAt, UpdatedAt
INSERT INTO Workspaces (Id, Name, Slug, OwnerId, CreatedAt, UpdatedAt)
VALUES 
    ('AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA', 'Engineering Team', 'engineering-team', '11111111-1111-1111-1111-111111111111', GETUTCDATE(), GETUTCDATE()),
    ('BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB', 'Marketing Team', 'marketing-team', '11111111-1111-1111-1111-111111111111', GETUTCDATE(), GETUTCDATE());

-- Workspace members
-- Schema: WorkspaceId, UserId, Role, JoinedAt
INSERT INTO WorkspaceMembers (WorkspaceId, UserId, Role, JoinedAt)
VALUES 
    ('AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA', '11111111-1111-1111-1111-111111111111', 'Owner', GETUTCDATE()),
    ('AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA', '22222222-2222-2222-2222-222222222222', 'Admin', GETUTCDATE()),
    ('AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA', '33333333-3333-3333-3333-333333333333', 'Member', GETUTCDATE()),
    ('AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA', '44444444-4444-4444-4444-444444444444', 'Guest', GETUTCDATE()),
    ('BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB', '11111111-1111-1111-1111-111111111111', 'Owner', GETUTCDATE());

-- Test projects
-- Schema: Id, WorkspaceId, Name, Key, Description, Status, CreatedAt, UpdatedAt
INSERT INTO Projects (Id, Name, [Key], Description, WorkspaceId, Status, CreatedAt, UpdatedAt)
VALUES 
    ('CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC', 'Backend API', 'BE', 'REST and GraphQL API development', 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA', 'Active', GETUTCDATE(), GETUTCDATE()),
    ('DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD', 'Web Frontend', 'WEB', 'React + Vite + MUI web app', 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA', 'Active', GETUTCDATE(), GETUTCDATE()),
    ('EEEEEEEE-EEEE-EEEE-EEEE-EEEEEEEEEEEE', 'Mobile App', 'MOB', 'Flutter mobile application', 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA', 'Active', GETUTCDATE(), GETUTCDATE());

-- Test boards
-- Schema: Id, ProjectId, Name, Order, CreatedAt
INSERT INTO Boards (Id, Name, [Order], ProjectId, CreatedAt)
VALUES 
    ('FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF', 'Sprint 1', 0, 'CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC', GETUTCDATE()),
    ('12121212-1212-1212-1212-121212121212', 'Sprint 2', 1, 'CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC', GETUTCDATE()),
    ('13131313-1313-1313-1313-131313131313', 'Sprint 1', 0, 'DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD', GETUTCDATE());

-- Test columns
-- Schema: Id, BoardId, Name, Order, WipLimit, CreatedAt
INSERT INTO Columns (Id, Name, [Order], BoardId, CreatedAt)
VALUES 
    ('14141414-1414-1414-1414-141414141414', 'Backlog', 0, 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF', GETUTCDATE()),
    ('15151515-1515-1515-1515-151515151515', 'In Progress', 1, 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF', GETUTCDATE()),
    ('16161616-1616-1616-1616-161616161616', 'Review', 2, 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF', GETUTCDATE()),
    ('17171717-1717-1717-1717-171717171717', 'Done', 3, 'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF', GETUTCDATE());

-- Test tasks
-- Schema: Id, BoardId, ColumnId, Title, Description, Status, Priority, AssigneeId, CreatorId, DueDate, Position, CreatedAt, UpdatedAt
INSERT INTO TaskItems (Id, BoardId, ColumnId, Title, Description, Status, Priority, Position, AssigneeId, CreatorId, CreatedAt, UpdatedAt, DueDate)
VALUES 
    ('18181818-1818-1818-1818-181818181818', 
     'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF',
     '15151515-1515-1515-1515-151515151515', 
     'Implement GraphQL schema', 
     'Create HotChocolate GraphQL schema with queries and mutations', 
     'InProgress', 'High', 0, 
     '22222222-2222-2222-2222-222222222222', 
     '11111111-1111-1111-1111-111111111111', 
     GETUTCDATE(), GETUTCDATE(), DATEADD(day, 7, GETUTCDATE())),
    
    ('19191919-1919-1919-1919-191919191919', 
     'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF',
     '15151515-1515-1515-1515-151515151515', 
     'Add DataLoaders for N+1 prevention', 
     'Implement AssigneeDataLoader and CommentDataLoader', 
     'InProgress', 'High', 1, 
     '22222222-2222-2222-2222-222222222222', 
     '11111111-1111-1111-1111-111111111111', 
     GETUTCDATE(), GETUTCDATE(), DATEADD(day, 7, GETUTCDATE())),
    
    ('20202020-2020-2020-2020-202020202020', 
     'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF',
     '14141414-1414-1414-1414-141414141414', 
     'Write unit tests for services', 
     'Add xUnit tests for WorkspaceService and ProjectService', 
     'Todo', 'Medium', 0, 
     '33333333-3333-3333-3333-333333333333', 
     '11111111-1111-1111-1111-111111111111', 
     GETUTCDATE(), GETUTCDATE(), DATEADD(day, 14, GETUTCDATE())),
    
    ('21212121-2121-2121-2121-212121212121', 
     'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF',
     '14141414-1414-1414-1414-141414141414', 
     'Setup JWT authentication', 
     'Implement JWT + Argon2 + Redis refresh tokens', 
     'Todo', 'High', 1, 
     '22222222-2222-2222-2222-222222222222', 
     '11111111-1111-1111-1111-111111111111', 
     GETUTCDATE(), GETUTCDATE(), DATEADD(day, 10, GETUTCDATE())),
    
    ('22222222-3333-3333-3333-333333333333', 
     'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF',
     '16161616-1616-1616-1616-161616161616', 
     'Code review for GraphQL implementation', 
     'Review PR for GraphQL layer', 
     'InProgress', 'Medium', 0, 
     '11111111-1111-1111-1111-111111111111', 
     '22222222-2222-2222-2222-222222222222', 
     GETUTCDATE(), GETUTCDATE(), DATEADD(day, 2, GETUTCDATE())),
    
    ('23232323-2323-2323-2323-232323232323', 
     'FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF',
     '17171717-1717-1717-1717-171717171717', 
     'Deploy to staging', 
     'Deploy backend API to Railway staging environment', 
     'Done', 'High', 0, 
     '22222222-2222-2222-2222-222222222222', 
     '11111111-1111-1111-1111-111111111111', 
     DATEADD(day, -5, GETUTCDATE()), GETUTCDATE(), DATEADD(day, -3, GETUTCDATE()));

-- Test comments
-- Schema: Id, TaskId, AuthorId, Body, CreatedAt, UpdatedAt
INSERT INTO Comments (Id, Body, TaskId, AuthorId, CreatedAt, UpdatedAt)
VALUES 
    ('24242424-2424-2424-2424-242424242424', 
     'Started working on the schema. Using code-first approach with HotChocolate.', 
     '18181818-1818-1818-1818-181818181818', 
     '22222222-2222-2222-2222-222222222222', 
     DATEADD(hour, -2, GETUTCDATE()), DATEADD(hour, -2, GETUTCDATE())),
    
    ('25252525-2525-2525-2525-252525252525', 
     'Good progress! Make sure to add filtering and sorting support.', 
     '18181818-1818-1818-1818-181818181818', 
     '11111111-1111-1111-1111-111111111111', 
     DATEADD(hour, -1, GETUTCDATE()), DATEADD(hour, -1, GETUTCDATE())),
    
    ('26262626-2626-2626-2626-262626262626', 
     'DataLoaders are crucial for performance. Will implement batch loading for assignees and comments.', 
     '19191919-1919-1919-1919-191919191919', 
     '22222222-2222-2222-2222-222222222222', 
     DATEADD(minute, -30, GETUTCDATE()), DATEADD(minute, -30, GETUTCDATE()));

-- Test notifications
-- Schema: Id, UserId, Type, Title, Body, TargetRef, ReadAt, CreatedAt
INSERT INTO Notifications (Id, UserId, Type, Title, Body, TargetRef, ReadAt, CreatedAt)
VALUES 
    ('27272727-2727-2727-2727-272727272727', 
     '22222222-2222-2222-2222-222222222222', 
     'TaskAssigned', 
     'Task Assigned',
     'You have been assigned to task: Implement GraphQL schema', 
     'task:18181818-1818-1818-1818-181818181818', 
     NULL,
     DATEADD(hour, -3, GETUTCDATE())),
    
    ('28282828-2828-2828-2828-282828282828', 
     '22222222-2222-2222-2222-222222222222', 
     'CommentAdded', 
     'New Comment',
     'Owner User commented on your task: Implement GraphQL schema', 
     'comment:25252525-2525-2525-2525-252525252525', 
     NULL,
     DATEADD(hour, -1, GETUTCDATE())),
    
    ('29292929-2929-2929-2929-292929292929', 
     '33333333-3333-3333-3333-333333333333', 
     'TaskAssigned', 
     'Task Assigned',
     'You have been assigned to task: Write unit tests for services', 
     'task:20202020-2020-2020-2020-202020202020', 
     DATEADD(hour, -4, GETUTCDATE()),
     DATEADD(hour, -5, GETUTCDATE()));

-- Test activity logs
-- Schema: Id, WorkspaceId, ActorId, EntityType, EntityId, Action, Payload, CreatedAt
INSERT INTO ActivityLogs (Id, WorkspaceId, ActorId, Action, EntityType, EntityId, Payload, CreatedAt)
VALUES 
    ('30303030-3030-3030-3030-303030303030', 
     'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA', 
     '11111111-1111-1111-1111-111111111111', 
     'Created', 
     'Project', 
     'CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC', 
     '{"name":"Backend API"}', 
     DATEADD(day, -10, GETUTCDATE())),
    
    ('31313131-3131-3131-3131-313131313131', 
     'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA', 
     '22222222-2222-2222-2222-222222222222', 
     'Created', 
     'Task', 
     '18181818-1818-1818-1818-181818181818', 
     '{"title":"Implement GraphQL schema","status":"InProgress"}', 
     DATEADD(hour, -3, GETUTCDATE())),
    
    ('32323232-3232-3232-3232-323232323232', 
     'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA', 
     '22222222-2222-2222-2222-222222222222', 
     'Updated', 
     'Task', 
     '23232323-2323-2323-2323-232323232323', 
     '{"status":"Done"}', 
     DATEADD(hour, -1, GETUTCDATE()));

PRINT 'Seed data inserted successfully!';
PRINT '';
PRINT 'Test users:';
PRINT '  owner@griot.test (Owner role)';
PRINT '  admin@griot.test (Admin role)';
PRINT '  member@griot.test (Member role)';
PRINT '  guest@griot.test (Guest role)';
PRINT '';
PRINT 'Workspace: Engineering Team (AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA)';
PRINT 'Project: Backend API (CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC)';
PRINT 'Board: Sprint 1 (FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF)';
PRINT '';
PRINT 'You can now test GraphQL queries at http://localhost:5064/graphql';
