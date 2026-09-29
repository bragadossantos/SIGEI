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
    GerirDefinicoesEscola,
    AdministrarPlataforma,
    ConsultarNotas
}

/// <summary>Matriz de permissões por perfil. A secretaria só gere o currículo se a escola o permitir.</summary>
public static class MatrizPermissoes
{
    public static bool Tem(PerfilUtilizador perfil, Permissao permissao, bool secretariaGereDisciplinas = false) =>
        perfil switch
        {
            PerfilUtilizador.AdminSaas => permissao == Permissao.AdministrarPlataforma, // escolas e licenças, nunca os dados pedagógicos
            PerfilUtilizador.Direcao => permissao is not (Permissao.LancarNotas or Permissao.ConsultarNotas or Permissao.AdministrarPlataforma),
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
