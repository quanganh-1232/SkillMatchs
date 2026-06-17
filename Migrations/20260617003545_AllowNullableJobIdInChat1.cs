using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SkillMatch.Migrations
{
    /// <inheritdoc />
    public partial class AllowNullableJobIdInChat1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Jobs_JobId",
                table: "ChatMessages");

            migrationBuilder.AlterColumn<int>(
                name: "JobId",
                table: "ChatMessages",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Jobs_JobId",
                table: "ChatMessages",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatMessages_Jobs_JobId",
                table: "ChatMessages");

            migrationBuilder.AlterColumn<int>(
                name: "JobId",
                table: "ChatMessages",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ChatMessages_Jobs_JobId",
                table: "ChatMessages",
                column: "JobId",
                principalTable: "Jobs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
