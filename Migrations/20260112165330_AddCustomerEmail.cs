using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KD_Restaurant.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PriceCost",
                table: "tblMenuItem",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "tblCustomer",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "tblBooking",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriceCost",
                table: "tblMenuItem");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "tblCustomer");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "tblBooking");
        }
    }
}
