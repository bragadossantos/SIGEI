using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sigei.Domain;
using Sigei.Web.Data;

namespace Sigei.Tests;

public sealed class IsolamentoEntreEscolasTests : IDisposable
{
    private sealed class Tenant(int? id) : ITenantAccessor
    {
        public int? EscolaId { get; } = id;
    }

    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private readonly DbContextOptions<ApplicationDbContext> _opcoes;

    public IsolamentoEntreEscolasTests()
    {
        _conn.Open();
        _opcoes = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_conn).Options;
        using var ctx = Contexto(null);
        ctx.Database.EnsureCreated();

        // Cada escola grava os seus próprios dados, com o seu contexto.
        foreach (var id in new[] { 1, 2 })
        {
            using var c = Contexto(id);
            c.Disciplinas.Add(new Disciplina { Nome = $"Matemática {id}" });
            c.SaveChanges();
        }
    }

    private ApplicationDbContext Contexto(int? escola) => new(_opcoes, new Tenant(escola));

    [Fact]
    public void Cada_escola_so_ve_os_seus_dados()
    {
        using var c1 = Contexto(1);
        using var c2 = Contexto(2);

        Assert.Equal(["Matemática 1"], c1.Disciplinas.Select(d => d.Nome).ToList());
        Assert.Equal(["Matemática 2"], c2.Disciplinas.Select(d => d.Nome).ToList());
    }

    [Fact]
    public void Sem_escola_identificada_nao_se_ve_nada()
    {
        using var semEscola = Contexto(null);
        Assert.Empty(semEscola.Disciplinas.ToList());
    }

    [Fact]
    public void Novos_registos_ficam_na_escola_do_utilizador_mesmo_que_venham_com_outra()
    {
        using (var c = Contexto(1))
        {
            c.Disciplinas.Add(new Disciplina { Nome = "Física", EscolaId = 2 }); // tentativa de gravar na escola 2
            c.SaveChanges();
        }

        using var c2 = Contexto(2);
        Assert.DoesNotContain(c2.Disciplinas.ToList(), d => d.Nome == "Física");
        using var c1 = Contexto(1);
        Assert.Contains(c1.Disciplinas.ToList(), d => d.Nome == "Física");
    }

    [Fact]
    public void Nao_se_muda_um_registo_de_escola()
    {
        using var c = Contexto(1);
        var d = c.Disciplinas.Single(x => x.Nome == "Matemática 1");
        d.EscolaId = 2;
        Assert.Throws<InvalidOperationException>(() => c.SaveChanges());
    }

    [Fact]
    public void Nao_se_acede_a_registo_de_outra_escola_pelo_id()
    {
        int idDaEscola2;
        using (var c2 = Contexto(2)) idDaEscola2 = c2.Disciplinas.Single().Id;

        using var c1 = Contexto(1);
        Assert.Null(c1.Disciplinas.Find(idDaEscola2) is { } d && d.EscolaId != 1 ? d : null);
        Assert.Null(c1.Disciplinas.FirstOrDefault(x => x.Id == idDaEscola2));
    }

    public void Dispose() => _conn.Dispose();
}
