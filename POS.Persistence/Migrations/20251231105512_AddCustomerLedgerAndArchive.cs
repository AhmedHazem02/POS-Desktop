using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerLedgerAndArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Customers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CustomerLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerLedgerEntries_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e445865-a24d-4543-a6c6-9443d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "05ab0efb-afb6-4f39-942b-e843255f6db4", "AQAAAAIAAYagAAAAEDQLDnBcal1X3gaL9771GVxvcbjI0oJKMtZg6ZAdf2YprGcRchZs8u8gx9Va0mRszQ==", "07c5642c-cf5a-4abb-9424-a50bbefdc5a9" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "5bfce730-a60a-43e0-bbd1-43df1ef62ca7", "AQAAAAIAAYagAAAAEOxEFX9eEwVilKJV1gl6pl2arZL4rero7w7jZJQaT0+c9So5elINY55Ts1CrjaJp4Q==", "fecd2732-070b-4bb7-9491-e5e547d177ae" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLedgerEntries_CustomerId",
                table: "CustomerLedgerEntries",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerLedgerEntries");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Customers");

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
    }
}
