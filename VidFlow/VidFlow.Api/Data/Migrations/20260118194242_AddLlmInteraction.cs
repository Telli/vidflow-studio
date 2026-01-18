using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VidFlow.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLlmInteraction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LlmInteractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SceneId = table.Column<Guid>(type: "uuid", nullable: true),
                    AgentRole = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    JobId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SystemPrompt = table.Column<string>(type: "text", nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: false),
                    Temperature = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    MaxTokens = table.Column<int>(type: "integer", nullable: false),
                    ResponseContent = table.Column<string>(type: "text", nullable: true),
                    InputTokens = table.Column<int>(type: "integer", nullable: false),
                    OutputTokens = table.Column<int>(type: "integer", nullable: false),
                    TotalTokens = table.Column<int>(type: "integer", nullable: false),
                    CostUsd = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DurationMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LlmInteractions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LlmInteractions_AgentRole",
                table: "LlmInteractions",
                column: "AgentRole");

            migrationBuilder.CreateIndex(
                name: "IX_LlmInteractions_CreatedAt",
                table: "LlmInteractions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LlmInteractions_JobId",
                table: "LlmInteractions",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmInteractions_ProjectId",
                table: "LlmInteractions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmInteractions_SceneId",
                table: "LlmInteractions",
                column: "SceneId");

            migrationBuilder.CreateIndex(
                name: "IX_LlmInteractions_Success",
                table: "LlmInteractions",
                column: "Success");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LlmInteractions");
        }
    }
}
