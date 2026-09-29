using Sigei.Domain;

namespace Sigei.Tests;

public class PermissoesTests
{
    [Fact]
    public void Secretaria_nao_gere_curriculo_por_defeito_mas_pode_se_a_escola_permitir()
    {
        Assert.False(MatrizPermissoes.Tem(PerfilUtilizador.Secretaria, Permissao.GerirCurriculoDisciplinas));
        Assert.True(MatrizPermissoes.Tem(PerfilUtilizador.Secretaria, Permissao.GerirCurriculoDisciplinas, secretariaGereDisciplinas: true));
    }

    [Theory]
    [InlineData(Permissao.AssociarDisciplinasATurmas)]
    [InlineData(Permissao.GerirMatriculasETurmas)]
    [InlineData(Permissao.DecidirAcessoNotas)]
    [InlineData(Permissao.GerirMensalidades)]
    public void Secretaria_faz_o_trabalho_administrativo(Permissao p) =>
        Assert.True(MatrizPermissoes.Tem(PerfilUtilizador.Secretaria, p));

    [Fact]
    public void Secretaria_nao_lanca_notas_nem_fecha_ano()
    {
        Assert.False(MatrizPermissoes.Tem(PerfilUtilizador.Secretaria, Permissao.LancarNotas, true));
        Assert.False(MatrizPermissoes.Tem(PerfilUtilizador.Secretaria, Permissao.AbrirFecharAnoLetivo, true));
    }

    [Fact]
    public void So_o_professor_lanca_notas_e_so_direcao_fecha_ano()
    {
        foreach (var p in Enum.GetValues<PerfilUtilizador>())
        {
            Assert.Equal(p == PerfilUtilizador.Professor, MatrizPermissoes.Tem(p, Permissao.LancarNotas));
            Assert.Equal(p == PerfilUtilizador.Direcao, MatrizPermissoes.Tem(p, Permissao.AbrirFecharAnoLetivo));
        }
    }

    [Fact]
    public void Aluno_e_encarregado_so_consultam_notas()
    {
        foreach (var p in new[] { PerfilUtilizador.Aluno, PerfilUtilizador.Encarregado })
            foreach (var perm in Enum.GetValues<Permissao>())
                Assert.Equal(perm == Permissao.ConsultarNotas, MatrizPermissoes.Tem(p, perm));
    }
}

public class MensalidadeELicencaTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 20);

    private static Mensalidade M(DateOnly venc, DateOnly? paga = null) =>
        new() { Referencia = "x", ValorKz = 15000, Vencimento = venc, PagaEm = paga };

    [Fact]
    public void Em_dia_quando_tudo_pago_ou_dentro_da_tolerancia()
    {
        Assert.True(Mensalidade.EmDia([M(new(2026, 9, 5), new(2026, 9, 6))], Hoje, 5));
        Assert.True(Mensalidade.EmDia([M(new(2026, 10, 16))], Hoje, 5)); // venceu há 4 dias
        Assert.True(Mensalidade.EmDia([], Hoje, 5));
    }

    [Fact]
    public void Em_atraso_depois_da_tolerancia()
    {
        Assert.False(Mensalidade.EmDia([M(new(2026, 10, 14))], Hoje, 5)); // venceu há 6 dias
    }

    [Fact]
    public void Matricula_aplica_a_politica_com_a_mensalidade_da_escola()
    {
        var escola = new Escola { Nome = "Colégio Teste" };
        var m = new Matricula { ClasseNumero = 5, Mensalidades = [M(new(2026, 9, 5))] };

        Assert.False(m.PodeVerNotas(PerfilAcesso.Encarregado, escola, Hoje).Permitido);
        m.ModoAcessoNotas = ModoAcessoNotas.Liberado;
        Assert.True(m.PodeVerNotas(PerfilAcesso.Encarregado, escola, Hoje).Permitido);
    }

    [Theory]
    [InlineData(2026, 12, 31, EstadoLicenca.Valida)]
    [InlineData(2026, 10, 1, EstadoLicenca.SoLeitura)]  // expirou há 19 dias
    [InlineData(2026, 8, 1, EstadoLicenca.Expirada)]    // expirou há mais de 30
    public void Estado_da_licenca(int a, int m, int d, EstadoLicenca esperado) =>
        Assert.Equal(esperado, new Escola { Nome = "x", LicencaValidaAte = new(a, m, d) }.EstadoLicenca(Hoje));

    [Fact]
    public void Escola_suspensa_nao_tem_acesso()
    {
        var e = new Escola { Nome = "x", Ativa = false, LicencaValidaAte = new(2027, 1, 1) };
        Assert.Equal(EstadoLicenca.Suspensa, e.EstadoLicenca(Hoje));
    }
}
