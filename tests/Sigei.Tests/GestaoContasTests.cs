using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sigei.Domain;
using Sigei.Web.Data;

namespace Sigei.Tests;

public sealed class GestaoContasTests : IDisposable
{
    private sealed class Tenant(int? id) : ITenantAccessor
    {
        public int? EscolaId { get; } = id;
    }

    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private readonly ServiceProvider _sp;

    private static readonly Ator Direcao1 = new("dir1", PerfilUtilizador.Direcao, 1);
    private static readonly Ator Secretaria1 = new("sec1", PerfilUtilizador.Secretaria, 1);
    private static readonly Ator Admin = new("admin", PerfilUtilizador.AdminSaas, null);

    public GestaoContasTests()
    {
        _conn.Open();
        var sc = new ServiceCollection();
        sc.AddLogging();
        sc.AddDataProtection();
        sc.AddSingleton<ITenantAccessor>(new Tenant(1));
        sc.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(_conn));
        sc.AddIdentityCore<ApplicationUser>(ConfiguracaoIdentity.Aplicar).AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
        sc.AddScoped<GestaoContas>();
        _sp = sc.BuildServiceProvider();
        using var scope = _sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
    }

    private async Task<T> Correr<T>(Func<GestaoContas, UserManager<ApplicationUser>, Task<T>> f)
    {
        using var scope = _sp.CreateScope();
        return await f(scope.ServiceProvider.GetRequiredService<GestaoContas>(),
                       scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>());
    }

    [Fact]
    public async Task Secretaria_cria_encarregado_com_telefone_e_palavrapasse_temporaria()
    {
        var r = await Correr((c, _) => c.CriarAsync(Secretaria1, "+244 923 456 789", "Maria Kiala", PerfilUtilizador.Encarregado, 1));
        Assert.True(r.Sucesso, r.Erro);
        Assert.Equal("923456789", r.Utilizador);

        await Correr(async (_, um) =>
        {
            var u = (await um.FindByNameAsync("923456789"))!;
            Assert.True(u.MudarPalavrapasse);
            Assert.Equal(1, u.EscolaId);
            Assert.Equal(PerfilUtilizador.Encarregado, u.Perfil);
            Assert.True(await um.CheckPasswordAsync(u, r.Palavrapasse!));
            return 0;
        });
    }

    [Fact]
    public async Task Secretaria_nao_cria_professor_nem_conta_noutra_escola()
    {
        var professor = await Correr((c, _) => c.CriarAsync(Secretaria1, "prof.um", "Prof", PerfilUtilizador.Professor, 1));
        Assert.False(professor.Sucesso);

        var outraEscola = await Correr((c, _) => c.CriarAsync(Secretaria1, "923000001", "X", PerfilUtilizador.Encarregado, 2));
        Assert.False(outraEscola.Sucesso);
    }

    [Fact]
    public async Task Direcao_cria_professor_e_recusa_duplicados_e_nomes_invalidos()
    {
        Assert.True((await Correr((c, _) => c.CriarAsync(Direcao1, "Prof.Um", "Prof Um", PerfilUtilizador.Professor, 1))).Sucesso);
        Assert.False((await Correr((c, _) => c.CriarAsync(Direcao1, "prof.um", "Outro", PerfilUtilizador.Professor, 1))).Sucesso);
        Assert.False((await Correr((c, _) => c.CriarAsync(Direcao1, "x y", "Outro", PerfilUtilizador.Professor, 1))).Sucesso);
        Assert.False((await Correr((c, _) => c.CriarAsync(Direcao1, "outra.direcao", "Outra", PerfilUtilizador.Direcao, 1))).Sucesso);
    }

    [Fact]
    public async Task So_o_admin_da_plataforma_cria_direcao_em_qualquer_escola()
    {
        Assert.True((await Correr((c, _) => c.CriarAsync(Admin, "direcao.escola9", "Diretora", PerfilUtilizador.Direcao, 9))).Sucesso);
        Assert.False((await Correr((c, _) => c.CriarAsync(Admin, "prof.x", "P", PerfilUtilizador.Professor, 9))).Sucesso);
    }

    [Fact]
    public async Task Repor_palavrapasse_gera_nova_obriga_a_mudar_e_levanta_bloqueio()
    {
        var criada = await Correr((c, _) => c.CriarAsync(Secretaria1, "923111222", "Ana", PerfilUtilizador.Encarregado, 1));
        var id = criada.UserId!;
        await Correr(async (_, um) =>
        {
            var u = (await um.FindByIdAsync(id))!;
            u.MudarPalavrapasse = false;
            await um.UpdateAsync(u);
            await um.SetLockoutEndDateAsync(u, DateTimeOffset.UtcNow.AddMinutes(10)); // bloqueada por tentativas
            return 0;
        });

        var r = await Correr((c, _) => c.ReporPalavrapasseAsync(Secretaria1, id));
        Assert.True(r.Sucesso, r.Erro);
        Assert.NotEqual(criada.Palavrapasse, r.Palavrapasse);

        await Correr(async (_, um) =>
        {
            var u = (await um.FindByIdAsync(id))!;
            Assert.True(u.MudarPalavrapasse);
            Assert.False(await um.IsLockedOutAsync(u));
            Assert.True(await um.CheckPasswordAsync(u, r.Palavrapasse!));
            Assert.False(await um.CheckPasswordAsync(u, criada.Palavrapasse!));
            return 0;
        });
    }

    [Fact]
    public async Task Nao_se_gere_conta_de_outra_escola_nem_a_propria_nem_de_perfil_superior()
    {
        var deOutraEscola = await Correr((c, _) => c.CriarAsync(new Ator("dir2", PerfilUtilizador.Direcao, 2), "923777001", "Z", PerfilUtilizador.Encarregado, 2));
        Assert.False((await Correr((c, _) => c.ReporPalavrapasseAsync(Secretaria1, deOutraEscola.UserId!))).Sucesso);
        Assert.False((await Correr((c, _) => c.AlterarEstadoAsync(Direcao1, deOutraEscola.UserId!, false))).Sucesso);

        var direcao = await Correr((c, _) => c.CriarAsync(Admin, "direcao.um", "Dir", PerfilUtilizador.Direcao, 1));
        Assert.False((await Correr((c, _) => c.ReporPalavrapasseAsync(Secretaria1, direcao.UserId!))).Sucesso);
        Assert.False((await Correr((c, _) => c.ReporPalavrapasseAsync(new Ator(direcao.UserId!, PerfilUtilizador.Direcao, 1), direcao.UserId!))).Sucesso);
    }

    [Fact]
    public async Task Desativar_impede_o_acesso_e_reativar_devolve_o_acesso()
    {
        var criada = await Correr((c, _) => c.CriarAsync(Direcao1, "prof.dois", "Prof Dois", PerfilUtilizador.Professor, 1));
        Assert.True((await Correr((c, _) => c.AlterarEstadoAsync(Direcao1, criada.UserId!, false))).Sucesso);

        await Correr(async (_, um) =>
        {
            var u = (await um.FindByIdAsync(criada.UserId!))!;
            Assert.True(await um.IsLockedOutAsync(u));
            Assert.True(GestaoContas.EstaDesativada(u));
            return 0;
        });

        Assert.True((await Correr((c, _) => c.AlterarEstadoAsync(Direcao1, criada.UserId!, true))).Sucesso);
        await Correr(async (_, um) =>
        {
            var u = (await um.FindByIdAsync(criada.UserId!))!;
            Assert.False(await um.IsLockedOutAsync(u));
            return 0;
        });
    }

    [Fact]
    public void Palavras_passe_geradas_cumprem_a_politica()
    {
        for (var i = 0; i < 50; i++)
        {
            var p = GestaoContas.GerarPalavrapasse();
            Assert.True(p.Length >= 8 && p.Any(char.IsUpper) && p.Any(char.IsLower) && p.Any(char.IsDigit) && p.Any(c => !char.IsLetterOrDigit(c)), p);
        }
    }

    public void Dispose()
    {
        _sp.Dispose();
        _conn.Dispose();
    }
}
