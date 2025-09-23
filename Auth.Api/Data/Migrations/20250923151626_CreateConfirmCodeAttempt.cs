using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateConfirmCodeAttempt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailConfirmCodeAttemt",
                table: "TempApplicationUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailConfirmCodeBlock",
                table: "TempApplicationUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmCodeAttemt",
                table: "TempApplicationUsers");

            migrationBuilder.DropColumn(
                name: "IsEmailConfirmCodeBlock",
                table: "TempApplicationUsers");
        }
    }
}
