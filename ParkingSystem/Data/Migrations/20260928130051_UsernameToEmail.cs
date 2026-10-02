using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class UsernameToEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Username",
                table: "AppUsers",
                newName: "Email");

            migrationBuilder.RenameIndex(
                name: "IX_AppUsers_Username",
                table: "AppUsers",
                newName: "IX_AppUsers_Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Email",
                table: "AppUsers",
                newName: "Username");

            migrationBuilder.RenameIndex(
                name: "IX_AppUsers_Email",
                table: "AppUsers",
                newName: "IX_AppUsers_Username");
        }
    }
}
