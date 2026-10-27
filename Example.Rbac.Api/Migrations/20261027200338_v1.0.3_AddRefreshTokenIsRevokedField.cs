using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Example.Rbac.Api.Migrations
{
    /// <inheritdoc />
    public partial class v103_AddRefreshTokenIsRevokedField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "RefreshTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "RefreshTokens");
        }
    }
}
