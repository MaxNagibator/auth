using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTempApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TempApplicationUsers",
                table: "TempApplicationUsers");

            migrationBuilder.DropColumn(
                name: "UserNameOriginal",
                table: "TempApplicationUsers");

            migrationBuilder.RenameColumn(
                name: "EmailOriginal",
                table: "TempApplicationUsers",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserEmail",
                table: "TempApplicationUsers",
                newName: "Email");

            migrationBuilder.AlterColumn<string>(
                name: "EmailConfirmCode",
                table: "TempApplicationUsers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "TempApplicationUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "TempApplicationUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TempApplicationUsers",
                table: "TempApplicationUsers",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TempApplicationUsers",
                table: "TempApplicationUsers");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "TempApplicationUsers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "TempApplicationUsers");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "TempApplicationUsers",
                newName: "EmailOriginal");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "TempApplicationUsers",
                newName: "ApplicationUserEmail");

            migrationBuilder.AlterColumn<string>(
                name: "EmailConfirmCode",
                table: "TempApplicationUsers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "UserNameOriginal",
                table: "TempApplicationUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TempApplicationUsers",
                table: "TempApplicationUsers",
                column: "ApplicationUserEmail");
        }
    }
}
