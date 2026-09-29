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

public class GestaoEscolarTests
{
    [Fact]
    public void Iniciacao_exige_vacinas_e_as_outras_classes_exigem_certificado()
    {
        Assert.Contains(TipoDocumento.BoletimVacinas, Documentos.Exigidos(new Classe(0)));
        Assert.DoesNotContain(TipoDocumento.CertificadoAnterior, Documentos.Exigidos(new Classe(0)));
        Assert.Contains(TipoDocumento.CertificadoAnterior, Documentos.Exigidos(new Classe(8)));
        Assert.All(Enumerable.Range(0, 14), n => Assert.Contains(TipoDocumento.BilheteOuCedula, Documentos.Exigidos(new Classe(n))));
    }

    [Fact]
    public void Plano_de_mensalidades_comeca_no_mes_de_inicio_e_vence_no_dia_5()
    {
        var plano = PlanoMensalidades.Gerar(new DateOnly(2026, 9, 14), 10);
        Assert.Equal(10, plano.Count);
        Assert.Equal("2026-09", plano[0].Referencia);
        Assert.Equal(new DateOnly(2026, 9, 5), plano[0].Vencimento);
        Assert.Equal("2027-06", plano[^1].Referencia); // atravessa o fim do ano civil
    }

    [Fact]
    public void Validacao_do_ano_letivo()
    {
        Assert.Null(RegrasAnoLetivo.Validar("2026/2027", new(2026, 9, 1), new(2027, 7, 31)));
        Assert.NotNull(RegrasAnoLetivo.Validar("", new(2026, 9, 1), new(2027, 7, 31)));
        Assert.NotNull(RegrasAnoLetivo.Validar("x", new(2027, 7, 31), new(2026, 9, 1)));
        Assert.NotNull(RegrasAnoLetivo.Validar("x", new(2026, 9, 1), new(2026, 10, 1)));
    }

    [Fact]
    public void Definicoes_da_escola_so_pela_direcao()
    {
        foreach (var p in Enum.GetValues<PerfilUtilizador>())
            Assert.Equal(p == PerfilUtilizador.Direcao, MatrizPermissoes.Tem(p, Permissao.GerirDefinicoesEscola, true));
    }
}

public class AnunciosTests
{
    private static readonly DateOnly Hoje = new(2026, 10, 1);

    private static Anuncio A(DestinatariosAnuncio d = DestinatariosAnuncio.Todos, bool arquivado = false, DateOnly? expira = null) =>
        new() { Titulo = "t", Texto = "x", AutorUserId = "u", AutorNome = "n", Destinatarios = d, Arquivado = arquivado, ExpiraEm = expira };

    [Fact]
    public void Anuncio_para_todos_e_visto_por_toda_a_escola_menos_pelo_admin_da_plataforma()
    {
        foreach (var p in Enum.GetValues<PerfilUtilizador>())
            Assert.Equal(p != PerfilUtilizador.AdminSaas, A().VisivelPara(p));
    }

    [Fact]
    public void Anuncio_do_pessoal_nao_chega_a_familias_e_o_das_familias_nao_chega_a_professores()
    {
        Assert.True(A(DestinatariosAnuncio.Pessoal).VisivelPara(PerfilUtilizador.Professor));
        Assert.False(A(DestinatariosAnuncio.Pessoal).VisivelPara(PerfilUtilizador.Encarregado));
        Assert.False(A(DestinatariosAnuncio.Pessoal).VisivelPara(PerfilUtilizador.Aluno));
        Assert.True(A(DestinatariosAnuncio.AlunosEEncarregados).VisivelPara(PerfilUtilizador.Aluno));
        Assert.True(A(DestinatariosAnuncio.AlunosEEncarregados).VisivelPara(PerfilUtilizador.Encarregado));
        Assert.False(A(DestinatariosAnuncio.AlunosEEncarregados).VisivelPara(PerfilUtilizador.Professor));
    }

    [Fact]
    public void Arquivado_ou_expirado_deixa_de_estar_ativo()
    {
        Assert.True(A().Ativo(Hoje));
        Assert.True(A(expira: Hoje).Ativo(Hoje));            // ainda vale no último dia
        Assert.False(A(expira: Hoje.AddDays(-1)).Ativo(Hoje));
        Assert.False(A(arquivado: true).Ativo(Hoje));
    }

    [Theory]
    [InlineData("", "texto", false)]
    [InlineData("   ", "texto", false)]
    [InlineData("Título", "", false)]
    [InlineData("Título", "texto", true)]
    public void Validacao_basica(string titulo, string texto, bool valido) =>
        Assert.Equal(valido, Anuncio.Validar(titulo, texto, null, Hoje) is null);

    [Fact]
    public void Validacao_de_limites_e_datas()
    {
        Assert.NotNull(Anuncio.Validar(new string('a', 121), "x", null, Hoje));
        Assert.NotNull(Anuncio.Validar("t", new string('a', 4001), null, Hoje));
        Assert.NotNull(Anuncio.Validar("t", "x", Hoje.AddDays(-1), Hoje));
        Assert.Null(Anuncio.Validar("t", "x", Hoje, Hoje));
    }

    [Fact]
    public void Publicar_e_so_da_direcao_e_da_secretaria_se_a_escola_permitir()
    {
        foreach (var p in Enum.GetValues<PerfilUtilizador>())
            Assert.Equal(p == PerfilUtilizador.Direcao, MatrizPermissoes.Tem(p, Permissao.PublicarAnuncios));
        Assert.True(MatrizPermissoes.Tem(PerfilUtilizador.Secretaria, Permissao.PublicarAnuncios, secretariaPublicaAnuncios: true));
        Assert.False(MatrizPermissoes.Tem(PerfilUtilizador.Professor, Permissao.PublicarAnuncios, true, true));
    }
}
