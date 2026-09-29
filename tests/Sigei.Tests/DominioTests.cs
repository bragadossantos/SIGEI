using Sigei.Domain;

namespace Sigei.Tests;

public class ClasseTests
{
    [Theory]
    [InlineData(0, NivelEnsino.Primario, false)]
    [InlineData(6, NivelEnsino.Primario, false)]
    [InlineData(7, NivelEnsino.PrimeiroCiclo, true)]
    [InlineData(9, NivelEnsino.PrimeiroCiclo, true)]
    [InlineData(10, NivelEnsino.SegundoCiclo, true)]
    [InlineData(13, NivelEnsino.SegundoCiclo, true)]
    public void Nivel_e_acesso_proprio_seguem_a_classe(int n, NivelEnsino nivel, bool acessoProprio)
    {
        var c = new Classe(n);
        Assert.Equal(nivel, c.Nivel);
        Assert.Equal(acessoProprio, c.AlunoTemAcessoProprio);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(14)]
    public void Classe_fora_do_intervalo_e_rejeitada(int n) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Classe(n));
}

public class NotasTests
{
    [Fact]
    public void Mt_e_a_media_de_mac_npp_npt_arredondada()
    {
        Assert.Equal(13m, new NotasTrimestre(12, 13, 14).Mt);
        Assert.Equal(12m, new NotasTrimestre(11, 12, 13.5m).Mt); // 12.16 -> 12
        Assert.Equal(11m, new NotasTrimestre(10, 11, 12.5m).Mt); // 11.16 -> 11
    }

    [Fact]
    public void Mt_so_existe_com_as_tres_notas()
    {
        Assert.Null(new NotasTrimestre(12, null, 14).Mt);
        Assert.Null(new NotasTrimestre(12, null, 14).Positiva);
    }

    [Fact]
    public void Positiva_a_partir_de_10()
    {
        Assert.True(new NotasTrimestre(10, 10, 10).Positiva);
        Assert.False(new NotasTrimestre(9, 9, 9).Positiva);
    }

    [Fact]
    public void Media_final_precisa_dos_tres_trimestres()
    {
        var t1 = new NotasTrimestre(10, 10, 10);
        var t2 = new NotasTrimestre(12, 12, 12);
        var t3 = new NotasTrimestre(14, 14, 14);
        Assert.Equal(12m, NotasTrimestre.MediaFinal(t1, t2, t3));
        Assert.Null(NotasTrimestre.MediaFinal(t1, t2, new NotasTrimestre(null, null, null)));
    }
}

public class PoliticaAcessoNotasTests
{
    private static DecisaoAcesso Av(PerfilAcesso p, int classe, ModoAcessoNotas m, bool emDia, bool regraAtiva = true) =>
        PoliticaAcessoNotas.Avaliar(p, new Classe(classe), m, emDia, regraAtiva);

    [Fact]
    public void Encarregado_com_mensalidade_em_dia_ve()
    {
        Assert.True(Av(PerfilAcesso.Encarregado, 5, ModoAcessoNotas.Automatico, true).Permitido);
    }

    [Fact]
    public void Encarregado_com_mensalidade_em_atraso_nao_ve()
    {
        var d = Av(PerfilAcesso.Encarregado, 5, ModoAcessoNotas.Automatico, false);
        Assert.False(d.Permitido);
        Assert.Equal(MotivoAcesso.MensalidadeEmAtraso, d.Motivo);
    }

    [Fact]
    public void Secretaria_pode_liberar_apesar_do_atraso()
    {
        var d = Av(PerfilAcesso.Encarregado, 5, ModoAcessoNotas.Liberado, false);
        Assert.True(d.Permitido);
        Assert.Equal(MotivoAcesso.ConsultaLiberadaPelaSecretaria, d.Motivo);
    }

    [Fact]
    public void Secretaria_pode_bloquear_apesar_de_estar_em_dia()
    {
        Assert.False(Av(PerfilAcesso.Encarregado, 5, ModoAcessoNotas.Bloqueado, true).Permitido);
        Assert.False(Av(PerfilAcesso.Aluno, 9, ModoAcessoNotas.Bloqueado, true).Permitido);
    }

    [Fact]
    public void Escola_pode_desligar_a_regra_da_mensalidade()
    {
        Assert.True(Av(PerfilAcesso.Encarregado, 5, ModoAcessoNotas.Automatico, false, regraAtiva: false).Permitido);
    }

    [Fact]
    public void Aluno_nao_depende_da_mensalidade_mas_so_a_partir_da_7()
    {
        Assert.True(Av(PerfilAcesso.Aluno, 7, ModoAcessoNotas.Automatico, false).Permitido);
        var d = Av(PerfilAcesso.Aluno, 6, ModoAcessoNotas.Automatico, true);
        Assert.False(d.Permitido);
        Assert.Equal(MotivoAcesso.AlunoSemAcessoProprio, d.Motivo);
    }
}
