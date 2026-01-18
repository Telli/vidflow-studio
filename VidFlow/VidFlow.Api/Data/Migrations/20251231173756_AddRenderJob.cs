using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VidFlow.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRenderJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RenderJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    ArtifactPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SourceVersion = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RenderJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RenderJobs_ProjectId",
                table: "RenderJobs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RenderJobs_SceneId",
                table: "RenderJobs",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_RenderJobs_Status",
                table: "RenderJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RenderJobs");
        }
    }
}
