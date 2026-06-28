using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillMatch.Migrations
{
    /// <inheritdoc />
    public partial class RemoveJobForeignKeyInChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Jobs_JobId",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_JobId",
                table: "ChatMessages");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_JobId",
                table: "ChatMessages",
                column: "JobId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Jobs_JobId",
                table: "ChatMessages",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id");
        }
    }
}
