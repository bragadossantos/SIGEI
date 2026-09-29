using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Sigei.Domain;
using Sigei.Web.Data;

namespace Sigei.Tests;

public sealed class AutorizacaoTests : IDisposable
{
    private sealed class Tenant(int? id) : ITenantAccessor
    {
        public int? EscolaId { get; } = id;
    }

    private static readonly DateOnly Hoje = DateOnly.FromDateTime(DateTime.Today);
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private readonly DbContextOptions<ApplicationDbContext> _opcoes;

    public AutorizacaoTests()
    {
        _conn.Open();
        _opcoes = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_conn).Options;
        using var db = Novo();
        db.Database.EnsureCreated();
        db.Escolas.AddRange(
            new Escola { Nome = "Válida", LicencaValidaAte = Hoje.AddDays(10) },
            new Escola { Nome = "Carência", LicencaValidaAte = Hoje.AddDays(-5) },
            new Escola { Nome = "Expirada", LicencaValidaAte = Hoje.AddDays(-60) },
            new Escola { Nome = "Suspensa", LicencaValidaAte = Hoje.AddDays(100), Ativa = false },
            new Escola { Nome = "SecretariaCurriculo", LicencaValidaAte = Hoje.AddDays(10), SecretariaGereDisciplinas = true });
        db.SaveChanges();
    }

    private ApplicationDbContext Novo() => new(_opcoes, new Tenant(null));

    private async Task<bool> Autoriza(PerfilUtilizador perfil, int? escolaId, Permissao? permissao, bool mudarSenha = false)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "u"), new(SigeiClaims.Perfil, perfil.ToString()) };
        if (escolaId is { } e) claims.Add(new(SigeiClaims.EscolaId, e.ToString()));
        if (mudarSenha) claims.Add(new(SigeiClaims.MudarPalavrapasse, "1"));
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "teste"));

        var requisito = new PermissaoRequirement(permissao);
        var ctx = new AuthorizationHandlerContext([requisito], user, null);
        using var db = Novo();
        await new PermissaoHandler(db).HandleAsync(ctx);
        return ctx.HasSucceeded;
    }

    // Ids criados por ordem: 1 Válida, 2 Carência, 3 Expirada, 4 Suspensa, 5 SecretariaCurriculo
    [Theory]
    [InlineData(1, true)]    // válida
    [InlineData(2, true)]    // em carência: entra (em modo só leitura nas páginas)
    [InlineData(3, false)]   // expirada
    [InlineData(4, false)]   // suspensa
    public async Task Acesso_conforme_o_estado_da_licenca(int escola, bool esperado)
    {
        Assert.Equal(esperado, await Autoriza(PerfilUtilizador.Secretaria, escola, Permissao.GerirMatriculasETurmas));
        Assert.Equal(esperado, await Autoriza(PerfilUtilizador.Aluno, escola, Permissao.ConsultarNotas));
        Assert.Equal(esperado, await Autoriza(PerfilUtilizador.Encarregado, escola, null)); // membro da escola
    }

    [Fact]
    public async Task Perfil_sem_permissao_e_recusado_mesmo_com_licenca_valida()
    {
        Assert.False(await Autoriza(PerfilUtilizador.Aluno, 1, Permissao.LancarNotas));
        Assert.False(await Autoriza(PerfilUtilizador.Secretaria, 1, Permissao.AbrirFecharAnoLetivo));
        Assert.False(await Autoriza(PerfilUtilizador.Direcao, 1, Permissao.AdministrarPlataforma));
    }

    [Fact]
    public async Task Secretaria_so_gere_curriculo_se_a_escola_permitir()
    {
        Assert.False(await Autoriza(PerfilUtilizador.Secretaria, 1, Permissao.GerirCurriculoDisciplinas));
        Assert.True(await Autoriza(PerfilUtilizador.Secretaria, 5, Permissao.GerirCurriculoDisciplinas));
    }

    [Fact]
    public async Task Admin_da_plataforma_so_administra_e_nao_precisa_de_escola()
    {
        Assert.True(await Autoriza(PerfilUtilizador.AdminSaas, null, Permissao.AdministrarPlataforma));
        Assert.False(await Autoriza(PerfilUtilizador.AdminSaas, null, Permissao.ConsultarNotas));
        Assert.False(await Autoriza(PerfilUtilizador.AdminSaas, null, null));
    }

    [Fact]
    public async Task Sem_escola_ou_com_escola_inexistente_nao_ha_acesso()
    {
        Assert.False(await Autoriza(PerfilUtilizador.Direcao, null, Permissao.GerirDefinicoesEscola));
        Assert.False(await Autoriza(PerfilUtilizador.Direcao, 999, Permissao.GerirDefinicoesEscola));
    }

    [Fact]
    public async Task Palavrapasse_temporaria_bloqueia_tudo_ate_ser_mudada()
    {
        Assert.False(await Autoriza(PerfilUtilizador.Direcao, 1, Permissao.GerirDefinicoesEscola, mudarSenha: true));
        Assert.False(await Autoriza(PerfilUtilizador.AdminSaas, null, Permissao.AdministrarPlataforma, mudarSenha: true));
        Assert.True(await Autoriza(PerfilUtilizador.Direcao, 1, Permissao.GerirDefinicoesEscola));
    }

    public void Dispose() => _conn.Dispose();
}
