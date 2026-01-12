using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KD_Restaurant.Migrations
{
    /// <inheritdoc />
    public partial class AddRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblRolePermission",
                columns: table => new
                {
                    IdRole = table.Column<int>(type: "int", nullable: false),
                    PermissionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsAllowed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblRolePermission", x => new { x.IdRole, x.PermissionKey });
                    table.ForeignKey(
                        name: "FK_tblRolePermission_tblRole_IdRole",
                        column: x => x.IdRole,
                        principalTable: "tblRole",
                        principalColumn: "IdRole",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblRolePermission");
        }
    }
}
