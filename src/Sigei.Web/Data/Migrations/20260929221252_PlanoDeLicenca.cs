using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigei.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlanoDeLicenca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LimiteAlunos",
                table: "Escolas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Plano",
                table: "Escolas",
                type: "TEXT",
                maxLength: 12,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LimiteAlunos",
                table: "Escolas");

            migrationBuilder.DropColumn(
                name: "Plano",
                table: "Escolas");
        }
    }
}
