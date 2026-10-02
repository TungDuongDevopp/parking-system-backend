using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixBug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_ParkingSpotId",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_VehicleId",
                table: "ParkingSessions");

            migrationBuilder.AlterColumn<long>(
                name: "SubscriptionId",
                table: "Payments",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "ParkingSessionId",
                table: "Payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentType",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<long>(
                name: "QuotationId",
                table: "ParkingSessions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<string>(
                name: "PlateNumber",
                table: "ParkingSessions",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SubscriptionId",
                table: "ParkingSessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketCode",
                table: "ParkingSessions",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ParkingSessionId",
                table: "Payments",
                column: "ParkingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_SubscriptionId",
                table: "ParkingSessions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_TicketCode",
                table: "ParkingSessions",
                column: "TicketCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ParkingSessions_ActiveParkingSpot",
                table: "ParkingSessions",
                column: "ParkingSpotId",
                unique: true,
                filter: "[ParkingSpotId] IS NOT NULL AND [ExitTime] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ParkingSessions_ActivePlateNumber",
                table: "ParkingSessions",
                column: "PlateNumber",
                unique: true,
                filter: "[PlateNumber] IS NOT NULL  AND [ExitTime] IS NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ParkingSessions_ActiveVehicle",
                table: "ParkingSessions",
                column: "VehicleId",
                unique: true,
                filter: "[VehicleId] IS NOT NULL AND [ExitTime] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSessions_Subscriptions_SubscriptionId",
                table: "ParkingSessions",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_ParkingSessions_ParkingSessionId",
                table: "Payments",
                column: "ParkingSessionId",
                principalTable: "ParkingSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_Subscriptions_SubscriptionId",
                table: "ParkingSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_ParkingSessions_ParkingSessionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ParkingSessionId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_SubscriptionId",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_TicketCode",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "UX_ParkingSessions_ActiveParkingSpot",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "UX_ParkingSessions_ActivePlateNumber",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "UX_ParkingSessions_ActiveVehicle",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "ParkingSessionId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "TicketCode",
                table: "ParkingSessions");

            migrationBuilder.AlterColumn<long>(
                name: "SubscriptionId",
                table: "Payments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "QuotationId",
                table: "ParkingSessions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PlateNumber",
                table: "ParkingSessions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_ParkingSpotId",
                table: "ParkingSessions",
                column: "ParkingSpotId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_VehicleId",
                table: "ParkingSessions",
                column: "VehicleId");
        }
    }
}
