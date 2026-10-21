using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ParkingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AutoCreateCustomer_SyncSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionAllowedPlate_Subscriptions_SubscriptionId",
                table: "SubscriptionAllowedPlate");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Email",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubscriptionAllowedPlate",
                table: "SubscriptionAllowedPlate");

            migrationBuilder.RenameTable(
                name: "SubscriptionAllowedPlate",
                newName: "SubscriptionAllowedPlates");

            migrationBuilder.RenameIndex(
                name: "IX_SubscriptionAllowedPlate_SubscriptionId_PlateNumber",
                table: "SubscriptionAllowedPlates",
                newName: "IX_SubscriptionAllowedPlates_SubscriptionId_PlateNumber");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Customers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Customers",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubscriptionAllowedPlates",
                table: "SubscriptionAllowedPlates",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers",
                column: "PhoneNumber",
                unique: true,
                filter: "[IsDeleted] = 0 AND [PhoneNumber] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionAllowedPlates_Subscriptions_SubscriptionId",
                table: "SubscriptionAllowedPlates",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionAllowedPlates_Subscriptions_SubscriptionId",
                table: "SubscriptionAllowedPlates");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Email",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubscriptionAllowedPlates",
                table: "SubscriptionAllowedPlates");

            migrationBuilder.RenameTable(
                name: "SubscriptionAllowedPlates",
                newName: "SubscriptionAllowedPlate");

            migrationBuilder.RenameIndex(
                name: "IX_SubscriptionAllowedPlates_SubscriptionId_PlateNumber",
                table: "SubscriptionAllowedPlate",
                newName: "IX_SubscriptionAllowedPlate_SubscriptionId_PlateNumber");

            migrationBuilder.AlterColumn<string>(
                name: "PhoneNumber",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Customers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Customers",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubscriptionAllowedPlate",
                table: "SubscriptionAllowedPlate",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true,
                filter: "[IsDeleted] = 0 AND [Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_PhoneNumber",
                table: "Customers",
                column: "PhoneNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionAllowedPlate_Subscriptions_SubscriptionId",
                table: "SubscriptionAllowedPlate",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
