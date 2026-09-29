using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AdduniqueIndexCurrentOcupied : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_CustomerId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_ParkingSpotId",
                table: "Reservations");

            migrationBuilder.AddColumn<int>(
                name: "CurrentOccupancy",
                table: "ParkingAreas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CustomerId",
                table: "Reservations",
                column: "CustomerId",
                unique: true,
                filter: "[Status] = 1 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ParkingSpotId",
                table: "Reservations",
                column: "ParkingSpotId",
                unique: true,
                filter: "[ParkingSpotId] IS NOT NULL AND [Status] = 1 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_CustomerId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_ParkingSpotId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "CurrentOccupancy",
                table: "ParkingAreas");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CustomerId",
                table: "Reservations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ParkingSpotId",
                table: "Reservations",
                column: "ParkingSpotId");
        }
    }
}
