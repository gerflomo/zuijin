using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zuijin.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkTokensToAuthorizationGrant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AuthorizationGrantId",
                table: "Tokens",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tokens_AuthorizationGrantId",
                table: "Tokens",
                column: "AuthorizationGrantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tokens_AuthorizationGrantId",
                table: "Tokens");

            migrationBuilder.DropColumn(
                name: "AuthorizationGrantId",
                table: "Tokens");
        }
    }
}
