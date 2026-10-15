using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ParkingTickets",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ParkingTickets");
        }
    }
}
