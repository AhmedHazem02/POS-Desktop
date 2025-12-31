using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInitialQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "InitialQuantity",
                table: "Products",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitialQuantity",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "8e445865-a24d-4543-a6c6-9443d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "1ce5d545-608c-4345-a810-70ea9f9904dc", "AQAAAAIAAYagAAAAEDpqwR+w3XNU3tedlC92J5QrXszxWHbDStcQxGc9hL6JZBhRjPgzm5GactxLJB6fcQ==", "732829fb-a12d-49fc-b04a-5db1abe1c56b" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "9e224968-33e4-4652-b7b7-8574d048cdb9",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "9aade6a3-d4c5-4e99-a911-fc029f080232", "AQAAAAIAAYagAAAAEI483lcu9CnQNgnJqeabQCXRnvKSJ2jYyG2H3zwtBPKdNUxY80PI1Ep2T2Hn/rmH3A==", "0966ef3e-d3cf-402a-8636-e1dda1f64cba" });
        }
    }
}
