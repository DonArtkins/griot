using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Griot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskItemBoardId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BoardId",
                table: "TaskItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Backfill legacy rows from the owning column's board (fresh DBs have no
            // rows; this guards pre-existing deployments where ColumnId is already set).
            migrationBuilder.Sql(
                "UPDATE TaskItems SET BoardId = c.BoardId FROM TaskItems t INNER JOIN Columns c ON c.Id = t.ColumnId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoardId",
                table: "TaskItems");
        }
    }
}
