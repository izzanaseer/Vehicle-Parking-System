using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ParkingSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVehiclesTicketsRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "SlotType",
                table: "ParkingSlots",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "VehicleRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<int>(type: "int", nullable: false),
                    RatePerHour = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParkingTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    SlotId = table.Column<int>(type: "int", nullable: false),
                    EntryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FeeAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingTickets_ParkingSlots_SlotId",
                        column: x => x.SlotId,
                        principalTable: "ParkingSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParkingTickets_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "SlotNumber", "SlotType" },
                values: new object[] { "C1", 0 });

            migrationBuilder.UpdateData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "SlotNumber", "SlotType" },
                values: new object[] { "C2", 0 });

            migrationBuilder.UpdateData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "SlotNumber", "SlotType" },
                values: new object[] { "C3", 0 });

            migrationBuilder.UpdateData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "SlotNumber", "SlotType" },
                values: new object[] { "B1", 1 });

            migrationBuilder.UpdateData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "SlotNumber", "SlotType" },
                values: new object[] { "B2", 1 });

            migrationBuilder.UpdateData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "SlotNumber", "SlotType" },
                values: new object[] { "T1", 2 });

            migrationBuilder.InsertData(
                table: "VehicleRates",
                columns: new[] { "Id", "RatePerHour", "Type" },
                values: new object[,]
                {
                    { 1, 50m, 0 },
                    { 2, 20m, 1 },
                    { 3, 100m, 2 }
                });
            
            migrationBuilder.InsertData(
                table: "ParkingSlots",
                columns: new[] { "Id", "SlotNumber", "SlotType", "IsOccupied" },
                values: new object[,]
                {
                    { 1, "C1", 0, false },
                    { 2, "C2", 0, false },
                    { 3, "C3", 0, false },
                    { 4, "B1", 1, false },
                    { 5, "B2", 1, false },
                    { 6, "T1", 2, false }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingTickets_SlotId",
                table: "ParkingTickets",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingTickets_VehicleId",
                table: "ParkingTickets",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleNumber",
                table: "Vehicles",
                column: "VehicleNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingTickets");

            migrationBuilder.DropTable(
                name: "VehicleRates");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropColumn(
                name: "SlotType",
                table: "ParkingSlots");

            migrationBuilder.UpdateData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 1,
                column: "SlotNumber",
                value: "S1");

            migrationBuilder.UpdateData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 2,
                column: "SlotNumber",
                value: "S2");

            migrationBuilder.UpdateData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 3,
                column: "SlotNumber",
                value: "S3");

            migrationBuilder.UpdateData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 4,
                column: "SlotNumber",
                value: "S4");

            migrationBuilder.UpdateData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 5,
                column: "SlotNumber",
                value: "S5");

            migrationBuilder.UpdateData(
                table: "ParkingSlots",
                keyColumn: "Id",
                keyValue: 6,
                column: "SlotNumber",
                value: "S6");

            migrationBuilder.InsertData(
                table: "ParkingSlots",
                columns: new[] { "Id", "IsOccupied", "SlotNumber" },
                values: new object[,]
                {
                    { 7, false, "S7" },
                    { 8, false, "S8" },
                    { 9, false, "S9" },
                    { 10, false, "S10" }
                });
        }
    }
}
