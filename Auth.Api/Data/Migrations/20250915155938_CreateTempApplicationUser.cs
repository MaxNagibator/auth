using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateTempApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmCode",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "TempApplicationUsers",
                columns: table => new
                {
                    ApplicationUserEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EmailConfirmCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    EmailOriginal = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UserNameOriginal = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TempApplicationUsers", x => x.ApplicationUserEmail);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TempApplicationUsers");

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmCode",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }
    }
}
