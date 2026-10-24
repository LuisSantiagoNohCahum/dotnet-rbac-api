using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Example.Rbac.Api.Migrations
{
    /// <inheritdoc />
    public partial class v101_UpdateRoleAndPermissionTableSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFullAccess",
                table: "Roles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Permissions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFullAccess",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Permissions");
        }
    }
}
