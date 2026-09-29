using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigei.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class GestaoEscolar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MensalidadeKz",
                table: "Turmas",
                type: "TEXT",
                precision: 14,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "NumeroMensalidades",
                table: "Escolas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "DocumentosMatricula",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    MatriculaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 24, nullable: false),
                    EntregueEm = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    Observacao = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosMatricula", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosMatricula_Matriculas_MatriculaId",
                        column: x => x.MatriculaId,
                        principalTable: "Matriculas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosMatricula_MatriculaId_Tipo",
                table: "DocumentosMatricula",
                columns: new[] { "MatriculaId", "Tipo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentosMatricula");

            migrationBuilder.DropColumn(
                name: "MensalidadeKz",
                table: "Turmas");

            migrationBuilder.DropColumn(
                name: "NumeroMensalidades",
                table: "Escolas");
        }
    }
}
