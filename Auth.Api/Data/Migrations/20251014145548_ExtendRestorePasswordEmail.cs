using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ExtendRestorePasswordEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                table: "RestorePasswordEmails",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CodeExpiresAt",
                table: "RestorePasswordEmails",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerificationCode",
                table: "RestorePasswordEmails",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Attempts",
                table: "RestorePasswordEmails");

            migrationBuilder.DropColumn(
                name: "CodeExpiresAt",
                table: "RestorePasswordEmails");

            migrationBuilder.DropColumn(
                name: "VerificationCode",
                table: "RestorePasswordEmails");
        }
    }
}
