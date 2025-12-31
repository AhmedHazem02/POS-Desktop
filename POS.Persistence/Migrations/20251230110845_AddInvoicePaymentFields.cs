using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoicePaymentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BillBreakdown",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CashierName",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChangeAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e445865-a24d-4543-a6c6-9443d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d3551d85-b325-41d4-b847-d18c55d7f590", "AQAAAAIAAYagAAAAEJTSLI8eqmIKp6aP6QmqHkhgY/QG3HDqzpLVDggp6A8Bp/sBcgBngbdl+fQXdSedCg==", "f2fa5e7e-84b0-489e-b748-bad64f6428de" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "85a2b532-d3c6-4654-9a19-69b246aca5ec", "AQAAAAIAAYagAAAAEDzIwWIDRMPE9lQAP94J1fP89lu8yvNqC734JBrgS3C0RDP0FUVFXBrXP1tU8XYS9w==", "3ea1ded0-9bc2-4a7b-a8bc-ce6cea4e700e" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "BillBreakdown",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CashierName",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ChangeAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Invoices");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e445865-a24d-4543-a6c6-9443d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "020b26a4-b60a-454d-85e2-a9d992524de2", "AQAAAAIAAYagAAAAEGDWB/a16FagOu2FbzkKh1jxgnn0Ghvs2Q+d8l/F3x39g3rNDqUUJpdZOHCdp+JaEg==", "5218add0-372b-435b-89ac-a675d812f846" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "8ff33344-ab40-4f91-b89e-5d7214e34f22", "AQAAAAIAAYagAAAAEG7O4ei+JtLpbTA3f3RgmjAAhhiJIrbfidaBaZdBj4nZzEYJJS4lDewsU/npK+qdSA==", "8c4f6e69-57fc-4dd2-9ac5-75b630cace3d" });
        }
    }
}
