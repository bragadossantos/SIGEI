using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sigei.Domain;
using Sigei.Web.Data;

namespace Sigei.Tests;

public sealed class BackupTests : IDisposable
{
    private sealed class Tenant(int? id) : ITenantAccessor
    {
        public int? EscolaId { get; } = id;
    }

    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private readonly DbContextOptions<ApplicationDbContext> _opcoes;
    private readonly string _pasta = Path.Combine(Path.GetTempPath(), "sigei-testes-" + Guid.NewGuid().ToString("N"));
    private readonly int[] _anos = new int[3];

    public BackupTests()
    {
        _conn.Open();
        _opcoes = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_conn).Options;
        using (var c = Ctx(null)) c.Database.EnsureCreated();

        using (var c = Ctx(null))
        {
            c.Escolas.AddRange(new Escola { Nome = "Escola Um" }, new Escola { Nome = "Escola Dois" });
            c.SaveChanges();
        }
        foreach (var escola in new[] { 1, 2 })
        {
            using var c = Ctx(escola);
            var ano = new AnoLetivo { Designacao = "2026/2027", Inicio = new(2026, 9, 1), Fim = new(2027, 7, 31) };
            c.AnosLetivos.Add(ano);
            c.SaveChanges();
            _anos[escola] = ano.Id;

            var turma = new Turma { AnoLetivoId = ano.Id, Nome = "5.ª A", ClasseNumero = 5, MensalidadeKz = 25000 };
            var disc = new Disciplina { Nome = "Matemática" };
            c.AddRange(turma, disc);
            c.SaveChanges();
            var td = new TurmaDisciplina { TurmaId = turma.Id, DisciplinaId = disc.Id };
            c.Add(td);
            var aluno = new Aluno { Nome = $"Aluno da escola {escola}", DataNascimento = new(2015, 1, 1) };
            aluno.Encarregados.Add(new Encarregado { Nome = $"Encarregado {escola}", Telefone = "923000000" });
            var mat = new Matricula { AnoLetivoId = ano.Id, Aluno = aluno, TurmaId = turma.Id, ClasseNumero = 5 };
            mat.Mensalidades.Add(new Mensalidade { Referencia = "2026-09", ValorKz = 25000, Vencimento = new(2026, 9, 5) });
            c.Add(mat);
            c.SaveChanges();
            c.Notas.Add(new Nota { MatriculaId = mat.Id, TurmaDisciplinaId = td.Id, Trimestre = Trimestre.Primeiro, Mac = 12, Npp = 13, Npt = 14 });
            c.SaveChanges();
        }
    }

    private ApplicationDbContext Ctx(int? escola) => new(_opcoes, new Tenant(escola));

    private BackupAnoLetivoService Servico(ApplicationDbContext c) =>
        new(c, Options.Create(new BackupOptions { Pasta = _pasta }));

    [Fact]
    public async Task Backup_contem_os_dados_da_escola_e_nada_de_outra()
    {
        using var c = Ctx(1);
        var r = await Servico(c).CriarAsync(1, _anos[1]);

        var json = await File.ReadAllTextAsync(r.CaminhoCompleto);
        using var doc = JsonDocument.Parse(json);
        var raiz = doc.RootElement;

        Assert.Equal("Escola Um", raiz.GetProperty("escola").GetProperty("nome").GetString());
        Assert.Contains("Aluno da escola 1", json);
        Assert.Contains("Encarregado 1", json);
        Assert.DoesNotContain("escola 2", json);
        Assert.DoesNotContain("Encarregado 2", json);
        Assert.Equal(1, raiz.GetProperty("notas").GetArrayLength());
        Assert.Equal(1, r.Notas);
        Assert.Equal(1, r.Alunos);
    }

    [Fact]
    public async Task Backup_grava_ficheiro_e_soma_de_verificacao_coerentes()
    {
        using var c = Ctx(1);
        var r = await Servico(c).CriarAsync(1, _anos[1]);

        var esperado = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(r.CaminhoCompleto))).ToLowerInvariant();
        Assert.Equal(esperado, r.Sha256);
        Assert.StartsWith(r.Sha256, await File.ReadAllTextAsync(r.CaminhoCompleto + ".sha256"));
        Assert.False(File.Exists(r.CaminhoCompleto + ".tmp"));
    }

    [Fact]
    public async Task Nao_se_cria_backup_de_outra_escola()
    {
        using var c = Ctx(1);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Servico(c).CriarAsync(2, _anos[2]));
    }

    [Theory]
    [InlineData("escola-1_2026-2027_20270801-100000.json", 1, true)]
    [InlineData("escola-1_2026-2027_20270801-100000.json", 2, false)]   // ficheiro de outra escola
    [InlineData("escola-10_2026-2027_x.json", 1, false)]                // prefixo parecido
    [InlineData("../escola-1_x.json", 1, false)]                        // sobe de pasta
    [InlineData("escola-1_x.json.sha256", 1, false)]                    // só o backup em si
    public void So_se_descarrega_ficheiro_da_propria_escola(string ficheiro, int escola, bool esperado) =>
        Assert.Equal(esperado, BackupAnoLetivoService.PertenceAEscola(ficheiro, escola));

    public void Dispose()
    {
        _conn.Dispose();
        if (Directory.Exists(_pasta)) Directory.Delete(_pasta, true);
    }
}
