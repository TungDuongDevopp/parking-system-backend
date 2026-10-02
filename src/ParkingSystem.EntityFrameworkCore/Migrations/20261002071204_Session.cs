using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSystem.Migrations
{
    /// <inheritdoc />
    public partial class Session : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CheckInStaffId",
                table: "ParkingSessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CheckOutStaffId",
                table: "ParkingSessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ParkingSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_CheckInStaffId",
                table: "ParkingSessions",
                column: "CheckInStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_CheckOutStaffId",
                table: "ParkingSessions",
                column: "CheckOutStaffId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSessions_Staffs_CheckInStaffId",
                table: "ParkingSessions",
                column: "CheckInStaffId",
                principalTable: "Staffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSessions_Staffs_CheckOutStaffId",
                table: "ParkingSessions",
                column: "CheckOutStaffId",
                principalTable: "Staffs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_Staffs_CheckInStaffId",
                table: "ParkingSessions");

            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_Staffs_CheckOutStaffId",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_CheckInStaffId",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSessions_CheckOutStaffId",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "CheckInStaffId",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "CheckOutStaffId",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ParkingSessions");
        }
    }
}
