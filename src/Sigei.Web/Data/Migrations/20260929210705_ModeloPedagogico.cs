using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigei.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModeloPedagogico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EscolaId",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeCompleto",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Perfil",
                table: "AspNetUsers",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Alteracoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Entidade = table.Column<string>(type: "TEXT", nullable: false),
                    EntidadeId = table.Column<string>(type: "TEXT", nullable: false),
                    Acao = table.Column<string>(type: "TEXT", nullable: false),
                    ValorAnterior = table.Column<string>(type: "TEXT", nullable: true),
                    ValorNovo = table.Column<string>(type: "TEXT", nullable: true),
                    QuandoUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alteracoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Alunos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    DataNascimento = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alunos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnosLetivos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Designacao = table.Column<string>(type: "TEXT", nullable: false),
                    Inicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Fim = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    BackupFeitoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnosLetivos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Disciplinas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disciplinas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Encarregados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encarregados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Escolas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    LicencaValidaAte = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false),
                    BloqueioPorMensalidadeAtivo = table.Column<bool>(type: "INTEGER", nullable: false),
                    SecretariaGereDisciplinas = table.Column<bool>(type: "INTEGER", nullable: false),
                    DiasToleranciaMensalidade = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Escolas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    MatriculaId = table.Column<int>(type: "INTEGER", nullable: false),
                    TurmaDisciplinaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Trimestre = table.Column<int>(type: "INTEGER", nullable: false),
                    Mac = table.Column<decimal>(type: "TEXT", precision: 4, scale: 1, nullable: true),
                    Npp = table.Column<decimal>(type: "TEXT", precision: 4, scale: 1, nullable: true),
                    Npt = table.Column<decimal>(type: "TEXT", precision: 4, scale: 1, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Turmas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    AnoLetivoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", nullable: false),
                    ClasseNumero = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Turmas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TurmasDisciplinas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    TurmaId = table.Column<int>(type: "INTEGER", nullable: false),
                    DisciplinaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProfessorUserId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurmasDisciplinas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlunoEncarregado",
                columns: table => new
                {
                    EducandosId = table.Column<int>(type: "INTEGER", nullable: false),
                    EncarregadosId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlunoEncarregado", x => new { x.EducandosId, x.EncarregadosId });
                    table.ForeignKey(
                        name: "FK_AlunoEncarregado_Alunos_EducandosId",
                        column: x => x.EducandosId,
                        principalTable: "Alunos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlunoEncarregado_Encarregados_EncarregadosId",
                        column: x => x.EncarregadosId,
                        principalTable: "Encarregados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matriculas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    AnoLetivoId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlunoId = table.Column<int>(type: "INTEGER", nullable: false),
                    TurmaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClasseNumero = table.Column<int>(type: "INTEGER", nullable: false),
                    ModoAcessoNotas = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matriculas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matriculas_Alunos_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Alunos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Matriculas_Turmas_TurmaId",
                        column: x => x.TurmaId,
                        principalTable: "Turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Mensalidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EscolaId = table.Column<int>(type: "INTEGER", nullable: false),
                    MatriculaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Referencia = table.Column<string>(type: "TEXT", nullable: false),
                    ValorKz = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Vencimento = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PagaEm = table.Column<DateOnly>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensalidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mensalidades_Matriculas_MatriculaId",
                        column: x => x.MatriculaId,
                        principalTable: "Matriculas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alteracoes_EscolaId_QuandoUtc",
                table: "Alteracoes",
                columns: new[] { "EscolaId", "QuandoUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AlunoEncarregado_EncarregadosId",
                table: "AlunoEncarregado",
                column: "EncarregadosId");

            migrationBuilder.CreateIndex(
                name: "IX_AnosLetivos_EscolaId_Designacao",
                table: "AnosLetivos",
                columns: new[] { "EscolaId", "Designacao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Disciplinas_EscolaId_Nome",
                table: "Disciplinas",
                columns: new[] { "EscolaId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Escolas_Nome",
                table: "Escolas",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_AlunoId",
                table: "Matriculas",
                column: "AlunoId");

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_AnoLetivoId_AlunoId",
                table: "Matriculas",
                columns: new[] { "AnoLetivoId", "AlunoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_TurmaId",
                table: "Matriculas",
                column: "TurmaId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensalidades_MatriculaId_Referencia",
                table: "Mensalidades",
                columns: new[] { "MatriculaId", "Referencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notas_MatriculaId_TurmaDisciplinaId_Trimestre",
                table: "Notas",
                columns: new[] { "MatriculaId", "TurmaDisciplinaId", "Trimestre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Turmas_AnoLetivoId_Nome",
                table: "Turmas",
                columns: new[] { "AnoLetivoId", "Nome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TurmasDisciplinas_TurmaId_DisciplinaId",
                table: "TurmasDisciplinas",
                columns: new[] { "TurmaId", "DisciplinaId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alteracoes");

            migrationBuilder.DropTable(
                name: "AlunoEncarregado");

            migrationBuilder.DropTable(
                name: "AnosLetivos");

            migrationBuilder.DropTable(
                name: "Disciplinas");

            migrationBuilder.DropTable(
                name: "Escolas");

            migrationBuilder.DropTable(
                name: "Mensalidades");

            migrationBuilder.DropTable(
                name: "Notas");

            migrationBuilder.DropTable(
                name: "TurmasDisciplinas");

            migrationBuilder.DropTable(
                name: "Encarregados");

            migrationBuilder.DropTable(
                name: "Matriculas");

            migrationBuilder.DropTable(
                name: "Alunos");

            migrationBuilder.DropTable(
                name: "Turmas");

            migrationBuilder.DropColumn(
                name: "EscolaId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NomeCompleto",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Perfil",
                table: "AspNetUsers");
        }
    }
}
