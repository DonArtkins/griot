using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Griot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddObservabilityAuditRequestIdAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "AuditLogs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_RequestId",
                table: "ErrorLogs",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId_CreatedAt",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_RequestId",
                table: "AuditLogs",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiLogs_CreatedAt",
                table: "ApiLogs",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ErrorLogs_RequestId",
                table: "ErrorLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_EntityType_EntityId_CreatedAt",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_RequestId",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_ApiLogs_CreatedAt",
                table: "ApiLogs");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "AuditLogs");
        }
    }
}
