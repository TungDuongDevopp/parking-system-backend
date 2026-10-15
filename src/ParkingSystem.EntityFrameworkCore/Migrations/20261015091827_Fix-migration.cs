using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSystem.Migrations
{
    /// <inheritdoc />
    public partial class Fixmigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_SubscriptionId",
                table: "ParkingSessions");

            migrationBuilder.AlterColumn<decimal>(
                name: "Fee",
                table: "ParkingSessions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_SubscriptionId",
                table: "ParkingSessions",
                column: "SubscriptionId",
                unique: true,
                filter: "[SubscriptionId] IS NOT NULL AND [Status] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_SubscriptionId",
                table: "ParkingSessions");

            migrationBuilder.AlterColumn<decimal>(
                name: "Fee",
                table: "ParkingSessions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_SubscriptionId",
                table: "ParkingSessions",
                column: "SubscriptionId");
        }
    }
}
