using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Griot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenantColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Workspaces",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "PlatformRole",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "TaskItems",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Projects",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Invites",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ErrorLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Comments",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Columns",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Boards",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Attachments",
                type: "uniqueidentifier",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ApiLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "ActivityLogs",
                type: "uniqueidentifier",
                nullable: false);

            // Spec 29 backfill (idempotent): one legacy organization per pre-existing
            // owner (workspace owners + activity actors), rows reassigned through the
            // ownership chain, leftovers quarantined under the 'legacy-quarantine'
            // org so nothing silently drops out of scope. Re-runs are no-ops.



            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlanName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OffboardedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organizations_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });


            migrationBuilder.CreateTable(
                name: "OrganizationLifecycleEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationLifecycleEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationLifecycleEvents_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
                    Permissions = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roles_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });


            migrationBuilder.CreateTable(
                name: "OrganizationInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InvitedById = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationInvites_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationInvites_Roles_CustomRoleId",
                        column: x => x.CustomRoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationInvites_Users_InvitedById",
                        column: x => x.InvitedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Roles_CustomRoleId",
                        column: x => x.CustomRoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM dbo.Organizations WHERE Slug = 'legacy-quarantine')
                BEGIN
                    INSERT INTO dbo.Organizations (Id, Name, Slug, OwnerId, Status, PlanName, CreatedAt, UpdatedAt, OffboardedAt)
                    SELECT TOP(1) NEWID(), 'Legacy Quarantine', 'legacy-quarantine', u.Id, 'Active', 'Free', SYSUTCDATETIME(), SYSUTCDATETIME(), NULL
                    FROM dbo.Users u;
                END;

                DECLARE @QuarantineId uniqueidentifier = (SELECT TOP(1) Id FROM dbo.Organizations WHERE Slug = 'legacy-quarantine');

                DECLARE @Owners TABLE (OwnerId uniqueidentifier NOT NULL PRIMARY KEY);
                INSERT INTO @Owners (OwnerId)
                    SELECT DISTINCT OwnerId FROM dbo.Workspaces WHERE OrganizationId = '00000000-0000-0000-0000-000000000000'
                    UNION
                    SELECT DISTINCT ActorId FROM dbo.ActivityLogs WHERE OrganizationId = '00000000-0000-0000-0000-000000000000';

                INSERT INTO dbo.Organizations (Id, Name, Slug, OwnerId, Status, PlanName, CreatedAt, UpdatedAt, OffboardedAt)
                SELECT NEWID(),
                       'Legacy Company ' + LEFT(CONVERT(nvarchar(36), o.OwnerId), 8),
                       'legacy-' + LOWER(LEFT(CONVERT(nvarchar(36), o.OwnerId), 8)),
                       o.OwnerId, 'Active', 'Free', SYSUTCDATETIME(), SYSUTCDATETIME(), NULL
                FROM @Owners o
                WHERE NOT EXISTS (SELECT 1 FROM dbo.Organizations x WHERE x.OwnerId = o.OwnerId);

                UPDATE w SET w.OrganizationId = ISNULL(org.Id, @QuarantineId)
                FROM dbo.Workspaces w
                OUTER APPLY (SELECT TOP(1) x.Id FROM dbo.Organizations x WHERE x.OwnerId = w.OwnerId AND x.Slug <> 'legacy-quarantine') org
                WHERE w.OrganizationId = '00000000-0000-0000-0000-000000000000';

                UPDATE p SET p.OrganizationId = w.OrganizationId
                FROM dbo.Projects p JOIN dbo.Workspaces w ON w.Id = p.WorkspaceId
                WHERE p.OrganizationId = '00000000-0000-0000-0000-000000000000';

                UPDATE b SET b.OrganizationId = p.OrganizationId
                FROM dbo.Boards b JOIN dbo.Projects p ON p.Id = b.ProjectId
                WHERE b.OrganizationId = '00000000-0000-0000-0000-000000000000';

                UPDATE c SET c.OrganizationId = b.OrganizationId
                FROM dbo.Columns c JOIN dbo.Boards b ON b.Id = c.BoardId
                WHERE c.OrganizationId = '00000000-0000-0000-0000-000000000000';

                UPDATE t SET t.OrganizationId = c.OrganizationId
                FROM dbo.TaskItems t JOIN dbo.Columns c ON c.Id = t.ColumnId
                WHERE t.OrganizationId = '00000000-0000-0000-0000-000000000000';

                UPDATE cm SET cm.OrganizationId = t.OrganizationId
                FROM dbo.Comments cm JOIN dbo.TaskItems t ON t.Id = cm.TaskId
                WHERE cm.OrganizationId = '00000000-0000-0000-0000-000000000000';

                UPDATE a SET a.OrganizationId = t.OrganizationId
                FROM dbo.Attachments a JOIN dbo.TaskItems t ON t.Id = a.TaskId
                WHERE a.OrganizationId = '00000000-0000-0000-0000-000000000000';

                UPDATE i SET i.OrganizationId = w.OrganizationId
                FROM dbo.Invites i JOIN dbo.Workspaces w ON w.Id = i.WorkspaceId
                WHERE i.OrganizationId = '00000000-0000-0000-0000-000000000000';

                UPDATE n SET n.OrganizationId = ISNULL(w.OrganizationId, @QuarantineId)
                FROM dbo.Notifications n
                LEFT JOIN dbo.Workspaces w ON w.OwnerId = n.UserId
                WHERE n.OrganizationId = '00000000-0000-0000-0000-000000000000';

                UPDATE a SET a.OrganizationId = w.OrganizationId
                FROM dbo.ActivityLogs a JOIN dbo.Workspaces w ON w.Id = a.WorkspaceId
                WHERE a.OrganizationId = '00000000-0000-0000-0000-000000000000';

                INSERT INTO dbo.OrganizationMembers (Id, OrganizationId, UserId, Role, CustomRoleId, Status, JoinedAt)
                SELECT NEWID(), w.OrganizationId, w.OwnerId, 'Owner', NULL, 'Active', SYSUTCDATETIME()
                FROM dbo.Workspaces w
                WHERE NOT EXISTS (
                    SELECT 1 FROM dbo.OrganizationMembers m
                    WHERE m.OrganizationId = w.OrganizationId AND m.UserId = w.OwnerId);
            """);


            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_OrganizationId",
                table: "Workspaces",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_OrganizationId",
                table: "TaskItems",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganizationId",
                table: "Projects",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OrganizationId",
                table: "Notifications",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Invites_OrganizationId",
                table: "Invites",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_OrganizationId",
                table: "ErrorLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_OrganizationId",
                table: "Comments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Columns_OrganizationId",
                table: "Columns",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Boards_OrganizationId",
                table: "Boards",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OrganizationId",
                table: "AuditLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_OrganizationId",
                table: "Attachments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiLogs_OrganizationId",
                table: "ApiLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_OrganizationId",
                table: "ActivityLogs",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvites_CustomRoleId",
                table: "OrganizationInvites",
                column: "CustomRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvites_InvitedById",
                table: "OrganizationInvites",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvites_OrganizationId",
                table: "OrganizationInvites",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvites_Token",
                table: "OrganizationInvites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationLifecycleEvents_OrganizationId_CreatedAt",
                table: "OrganizationLifecycleEvents",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_CustomRoleId",
                table: "OrganizationMembers",
                column: "CustomRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_OrganizationId_UserId",
                table: "OrganizationMembers",
                columns: new[] { "OrganizationId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMembers_UserId",
                table: "OrganizationMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_OwnerId",
                table: "Organizations",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_OrganizationId",
                table: "Roles",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Organizations_OrganizationId",
                table: "Projects",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Workspaces_Organizations_OrganizationId",
                table: "Workspaces",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Organizations_OrganizationId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Workspaces_Organizations_OrganizationId",
                table: "Workspaces");

            migrationBuilder.DropTable(
                name: "OrganizationInvites");

            migrationBuilder.DropTable(
                name: "OrganizationLifecycleEvents");

            migrationBuilder.DropTable(
                name: "OrganizationMembers");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Workspaces_OrganizationId",
                table: "Workspaces");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_OrganizationId",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OrganizationId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_OrganizationId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Invites_OrganizationId",
                table: "Invites");

            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_OrganizationId",
                table: "ErrorLogs");

            migrationBuilder.DropIndex(
                name: "IX_Comments_OrganizationId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Columns_OrganizationId",
                table: "Columns");

            migrationBuilder.DropIndex(
                name: "IX_Boards_OrganizationId",
                table: "Boards");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_OrganizationId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_OrganizationId",
                table: "Attachments");

            migrationBuilder.DropIndex(
                name: "IX_ApiLogs_OrganizationId",
                table: "ApiLogs");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_OrganizationId",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "PlatformRole",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Invites");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ErrorLogs");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Columns");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Boards");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ApiLogs");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ActivityLogs");
        }
    }
}
