using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Griot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenFamilyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                table: "RefreshTokens",
                type: "uniqueidentifier",
                nullable: true);

            // Preserve existing rotation chains; a new family is not assigned to every row.
            migrationBuilder.Sql("""
                ;WITH Families AS (
                    SELECT r.Id, r.UserId, r.ReplacedByTokenId, r.Id AS FamilyId,
                        CAST(CONVERT(varchar(36), r.Id) AS varchar(max)) AS Visited
                    FROM dbo.RefreshTokens r
                    WHERE NOT EXISTS (
                        SELECT 1 FROM dbo.RefreshTokens parent
                        WHERE parent.ReplacedByTokenId = r.Id AND parent.UserId = r.UserId
                    )
                    UNION ALL
                    SELECT child.Id, child.UserId, child.ReplacedByTokenId, parent.FamilyId,
                        CAST(parent.Visited + ',' + CONVERT(varchar(36), child.Id) AS varchar(max))
                    FROM dbo.RefreshTokens child
                    JOIN Families parent ON child.Id = parent.ReplacedByTokenId
                        AND child.UserId = parent.UserId
                    WHERE CHARINDEX(CONVERT(varchar(36), child.Id), parent.Visited) = 0
                )
                UPDATE token SET FamilyId = family.FamilyId
                FROM dbo.RefreshTokens token JOIN Families family ON token.Id = family.Id
                OPTION (MAXRECURSION 0);

                -- Corrupt cycles have no root: invalidate those tokens rather than trust them.
                UPDATE dbo.RefreshTokens
                SET FamilyId = Id, RevokedAt = COALESCE(RevokedAt, SYSUTCDATETIME())
                WHERE FamilyId IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "FamilyId",
                table: "RefreshTokens",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_FamilyId",
                table: "RefreshTokens",
                columns: new[] { "UserId", "FamilyId" });

            // Auth repair amendment (Feature 07): email verification state (2FA / OTP flows).
            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerified",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_FamilyId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                table: "RefreshTokens");
        }
    }
}
