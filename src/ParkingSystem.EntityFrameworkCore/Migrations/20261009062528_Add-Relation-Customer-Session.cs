using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationCustomerSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_Quotations_QuotationId",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "EntryImageUrl",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "ExitImageUrl",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "ParkingSessions");

            migrationBuilder.DropIndex(
            name: "IX_ParkingSessions_QuotationId",
            table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "QuotationId",
                table: "ParkingSessions");

            migrationBuilder.AddColumn<long>(
                name: "CustomerId",
                table: "ParkingSessions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_CustomerId",
                table: "ParkingSessions",
                column: "CustomerId");

            migrationBuilder.AddColumn<decimal>(
                name: "DailyRate",
                table: "ParkingSessions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "ParkingSessions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSessions_Customers_CustomerId",
                table: "ParkingSessions",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSessions_Customers_CustomerId",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "DailyRate",
                table: "ParkingSessions");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "ParkingSessions");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "ParkingSessions",
                newName: "QuotationId");

            migrationBuilder.RenameIndex(
                name: "IX_ParkingSessions_CustomerId",
                table: "ParkingSessions",
                newName: "IX_ParkingSessions_QuotationId");

            migrationBuilder.AddColumn<string>(
                name: "EntryImageUrl",
                table: "ParkingSessions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ExitImageUrl",
                table: "ParkingSessions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "ParkingSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSessions_Quotations_QuotationId",
                table: "ParkingSessions",
                column: "QuotationId",
                principalTable: "Quotations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
