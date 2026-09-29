namespace Sigei.Domain;

public enum PerfilUtilizador
{
    AdminSaas,
    Direcao,
    Secretaria,
    Professor,
    Aluno,
    Encarregado
}

public enum Permissao
{
    GerirCurriculoDisciplinas,
    AssociarDisciplinasATurmas,
    GerirMatriculasETurmas,
    GerirDocumentosEAvisos,
    DecidirAcessoNotas,
    GerirMensalidades,
    LancarNotas,
    AbrirFecharAnoLetivo,
    ConsultarNotas
}

/// <summary>Matriz de permissões por perfil. A secretaria só gere o currículo se a escola o permitir.</summary>
public static class MatrizPermissoes
{
    public static bool Tem(PerfilUtilizador perfil, Permissao permissao, bool secretariaGereDisciplinas = false) =>
        perfil switch
        {
            PerfilUtilizador.AdminSaas => false, // administra escolas e licenças, não os dados pedagógicos
            PerfilUtilizador.Direcao => permissao != Permissao.LancarNotas && permissao != Permissao.ConsultarNotas,
            PerfilUtilizador.Secretaria => permissao switch
            {
                Permissao.AssociarDisciplinasATurmas
                    or Permissao.GerirMatriculasETurmas
                    or Permissao.GerirDocumentosEAvisos
                    or Permissao.DecidirAcessoNotas
                    or Permissao.GerirMensalidades => true,
                Permissao.GerirCurriculoDisciplinas => secretariaGereDisciplinas,
                _ => false
            },
            PerfilUtilizador.Professor => permissao == Permissao.LancarNotas,
            PerfilUtilizador.Aluno or PerfilUtilizador.Encarregado => permissao == Permissao.ConsultarNotas,
            _ => false
        };
}
