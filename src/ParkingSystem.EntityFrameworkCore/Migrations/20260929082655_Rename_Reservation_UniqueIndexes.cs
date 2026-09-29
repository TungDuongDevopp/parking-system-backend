using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSystem.Migrations
{
    /// <inheritdoc />
    public partial class Rename_Reservation_UniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Reservations_ParkingSpotId",
                table: "Reservations",
                newName: "UX_Reservations_ActiveParkingSpot");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_CustomerId",
                table: "Reservations",
                newName: "UX_Reservations_ActiveCustomer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "UX_Reservations_ActiveParkingSpot",
                table: "Reservations",
                newName: "IX_Reservations_ParkingSpotId");

            migrationBuilder.RenameIndex(
                name: "UX_Reservations_ActiveCustomer",
                table: "Reservations",
                newName: "IX_Reservations_CustomerId");
        }
    }
}
