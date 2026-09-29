using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigei.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ContasPorTelefone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MudarPalavrapasse",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MudarPalavrapasse",
                table: "AspNetUsers");
        }
    }
}
