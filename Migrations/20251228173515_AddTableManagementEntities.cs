using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KD_Restaurant.Migrations
{
    /// <inheritdoc />
    public partial class AddTableManagementEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "tblUser",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<int>(
                name: "IdRole",
                table: "tblRole",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.CreateTable(
                name: "tblTable_status",
                columns: table => new
                {
                    IdStatus = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblTable_status", x => x.IdStatus);
                });

            migrationBuilder.CreateTable(
                name: "tblTable_type",
                columns: table => new
                {
                    IdType = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TypeName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    MaxSeats = table.Column<int>(type: "int", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblTable_type", x => x.IdType);
                });

            migrationBuilder.CreateTable(
                name: "tblTables",
                columns: table => new
                {
                    IdTable = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TableName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdArea = table.Column<int>(type: "int", nullable: true),
                    IdType = table.Column<int>(type: "int", nullable: true),
                    IdStatus = table.Column<int>(type: "int", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblTables", x => x.IdTable);
                    table.ForeignKey(
                        name: "FK_tblTables_tblTable_status_IdStatus",
                        column: x => x.IdStatus,
                        principalTable: "tblTable_status",
                        principalColumn: "IdStatus",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tblTables_tblTable_type_IdType",
                        column: x => x.IdType,
                        principalTable: "tblTable_type",
                        principalColumn: "IdType",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tblBooking_IdTable",
                table: "tblBooking",
                column: "IdTable");

            migrationBuilder.CreateIndex(
                name: "IX_tblTables_IdStatus",
                table: "tblTables",
                column: "IdStatus");

            migrationBuilder.CreateIndex(
                name: "IX_tblTables_IdType",
                table: "tblTables",
                column: "IdType");

            migrationBuilder.AddForeignKey(
                name: "FK_tblBooking_tblTables_IdTable",
                table: "tblBooking",
                column: "IdTable",
                principalTable: "tblTables",
                principalColumn: "IdTable",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tblBooking_tblTables_IdTable",
                table: "tblBooking");

            migrationBuilder.DropTable(
                name: "tblTables");

            migrationBuilder.DropTable(
                name: "tblTable_status");

            migrationBuilder.DropTable(
                name: "tblTable_type");

            migrationBuilder.DropIndex(
                name: "IX_tblBooking_IdTable",
                table: "tblBooking");

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "tblUser",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512);

            migrationBuilder.AlterColumn<int>(
                name: "IdRole",
                table: "tblRole",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");
        }
    }
}
