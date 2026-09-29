using Sigei.Domain;

namespace Sigei.Tests;

public class NomesDeUtilizadorTests
{
    [Theory]
    [InlineData("923 456 789", "923456789")]
    [InlineData("923-456-789", "923456789")]
    [InlineData("+244 923 456 789", "923456789")]
    [InlineData("244923456789", "923456789")]
    [InlineData("00244923456789", "923456789")]
    [InlineData("  Joana.Secretaria ", "joana.secretaria")]
    public void Normalizacao(string entrada, string esperado) =>
        Assert.Equal(esperado, NomesDeUtilizador.Normalizar(entrada));

    [Theory]
    [InlineData("923456789", true)]
    [InlineData("joana.secretaria", true)]
    [InlineData("ab", false)]                 // curto
    [InlineData("nome com espaços", false)]
    [InlineData("823456789", false)]          // telefone que não começa por 9
    [InlineData("12345", false)]              // só dígitos mas não é telefone
    [InlineData("", false)]
    [InlineData("<script>", false)]
    public void Validacao(string normalizado, bool valido) =>
        Assert.Equal(valido, NomesDeUtilizador.Validar(normalizado) is null);

    [Fact]
    public void Nome_de_utilizador_muito_longo_e_recusado() =>
        Assert.NotNull(NomesDeUtilizador.Validar(new string('a', 31)));
}

public class GestaoDeContasRegrasTests
{
    [Theory]
    [InlineData(PerfilUtilizador.AdminSaas, PerfilUtilizador.Direcao, true)]
    [InlineData(PerfilUtilizador.AdminSaas, PerfilUtilizador.Secretaria, false)]
    [InlineData(PerfilUtilizador.Direcao, PerfilUtilizador.Professor, true)]
    [InlineData(PerfilUtilizador.Direcao, PerfilUtilizador.Secretaria, true)]
    [InlineData(PerfilUtilizador.Direcao, PerfilUtilizador.Direcao, false)]
    [InlineData(PerfilUtilizador.Direcao, PerfilUtilizador.AdminSaas, false)]
    [InlineData(PerfilUtilizador.Secretaria, PerfilUtilizador.Encarregado, true)]
    [InlineData(PerfilUtilizador.Secretaria, PerfilUtilizador.Aluno, true)]
    [InlineData(PerfilUtilizador.Secretaria, PerfilUtilizador.Professor, false)]
    [InlineData(PerfilUtilizador.Secretaria, PerfilUtilizador.Direcao, false)]
    [InlineData(PerfilUtilizador.Professor, PerfilUtilizador.Aluno, false)]
    [InlineData(PerfilUtilizador.Encarregado, PerfilUtilizador.Encarregado, false)]
    public void Quem_pode_gerir_quem(PerfilUtilizador ator, PerfilUtilizador alvo, bool esperado) =>
        Assert.Equal(esperado, GestaoDeContasRegras.PodeGerir(ator, alvo));
}

public class AdministracaoDaPlataformaTests
{
    [Fact]
    public void So_o_admin_da_plataforma_administra_escolas_e_nunca_toca_nos_dados_pedagogicos()
    {
        foreach (var p in Enum.GetValues<PerfilUtilizador>())
            Assert.Equal(p == PerfilUtilizador.AdminSaas, MatrizPermissoes.Tem(p, Permissao.AdministrarPlataforma, true));

        foreach (var perm in Enum.GetValues<Permissao>().Where(x => x != Permissao.AdministrarPlataforma))
            Assert.False(MatrizPermissoes.Tem(PerfilUtilizador.AdminSaas, perm, true), $"AdminSaas não deve ter {perm}");
    }
}

public class RenovacaoDeLicencaTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 1);

    [Fact]
    public void Licenca_ainda_valida_soma_ao_fim_atual() =>
        Assert.Equal(new DateOnly(2027, 1, 31), Escola.RenovarValidade(new(2026, 12, 31), Hoje, 1));

    [Fact]
    public void Licenca_expirada_conta_a_partir_de_hoje() =>
        Assert.Equal(new DateOnly(2027, 1, 1), Escola.RenovarValidade(new(2026, 6, 30), Hoje, 3));

    [Fact]
    public void Renovar_por_um_ano() =>
        Assert.Equal(new DateOnly(2027, 10, 1), Escola.RenovarValidade(Hoje, Hoje, 12));
}
