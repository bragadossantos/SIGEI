using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigei.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Anuncios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SecretariaPublicaAnuncios",
                table: "Escolas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Anuncios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Texto = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Destinatarios = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Fixado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Arquivado = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExpiraEm = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    PublicadoEmUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AutorUserId = table.Column<string>(type: "TEXT", nullable: false),
                    AutorNome = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Anuncios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Anuncios_EscolaId_Arquivado_PublicadoEmUtc",
                table: "Anuncios",
                columns: new[] { "EscolaId", "Arquivado", "PublicadoEmUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Anuncios");

            migrationBuilder.DropColumn(
                name: "SecretariaPublicaAnuncios",
                table: "Escolas");
        }
    }
}
