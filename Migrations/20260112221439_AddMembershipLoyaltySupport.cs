using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KD_Restaurant.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipLoyaltySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "tblCustomer");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "tblCustomer");

            migrationBuilder.AddColumn<int>(
                name: "OriginalAmount",
                table: "tblOrder",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsEarned",
                table: "tblOrder",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PointsRedeemed",
                table: "tblOrder",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RedeemAmount",
                table: "tblOrder",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IdUser",
                table: "tblCustomer",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "tblContact",
                columns: table => new
                {
                    IdContact = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblContact", x => x.IdContact);
                });

            migrationBuilder.CreateTable(
                name: "tblMembershipCard",
                columns: table => new
                {
                    IdCard = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCustomer = table.Column<int>(type: "int", nullable: false),
                    CardNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblMembershipCard", x => x.IdCard);
                    table.ForeignKey(
                        name: "FK_tblMembershipCard_tblCustomer_IdCustomer",
                        column: x => x.IdCustomer,
                        principalTable: "tblCustomer",
                        principalColumn: "IdCustomer",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblPointHistory",
                columns: table => new
                {
                    IdHistory = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCard = table.Column<int>(type: "int", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblPointHistory", x => x.IdHistory);
                    table.ForeignKey(
                        name: "FK_tblPointHistory_tblMembershipCard_IdCard",
                        column: x => x.IdCard,
                        principalTable: "tblMembershipCard",
                        principalColumn: "IdCard",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblCustomer_IdUser",
                table: "tblCustomer",
                column: "IdUser",
                unique: true,
                filter: "[IdUser] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tblMembershipCard_CardNumber",
                table: "tblMembershipCard",
                column: "CardNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblMembershipCard_IdCustomer",
                table: "tblMembershipCard",
                column: "IdCustomer",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tblPointHistory_IdCard",
                table: "tblPointHistory",
                column: "IdCard");

            migrationBuilder.AddForeignKey(
                name: "FK_tblCustomer_tblUser_IdUser",
                table: "tblCustomer",
                column: "IdUser",
                principalTable: "tblUser",
                principalColumn: "IdUser",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblCustomer_tblUser_IdUser",
                table: "tblCustomer");

            migrationBuilder.DropTable(
                name: "tblContact");

            migrationBuilder.DropTable(
                name: "tblPointHistory");

            migrationBuilder.DropTable(
                name: "tblMembershipCard");

            migrationBuilder.DropIndex(
                name: "IX_tblCustomer_IdUser",
                table: "tblCustomer");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "tblOrder");

            migrationBuilder.DropColumn(
                name: "PointsEarned",
                table: "tblOrder");

            migrationBuilder.DropColumn(
                name: "PointsRedeemed",
                table: "tblOrder");

            migrationBuilder.DropColumn(
                name: "RedeemAmount",
                table: "tblOrder");

            migrationBuilder.DropColumn(
                name: "IdUser",
                table: "tblCustomer");

            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "tblCustomer",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "tblCustomer",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
