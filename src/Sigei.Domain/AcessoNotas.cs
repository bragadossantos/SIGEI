namespace Sigei.Domain;

public enum PerfilAcesso
{
    Aluno,
    Encarregado
}

/// <summary>Decisão da secretaria sobre o acesso às notas de uma matrícula.</summary>
public enum ModoAcessoNotas
{
    /// <summary>Segue a regra da escola: o encarregado só vê se a mensalidade estiver em dia.</summary>
    Automatico,
    /// <summary>A secretaria liberou o acesso, independentemente do pagamento.</summary>
    Liberado,
    /// <summary>A secretaria bloqueou o acesso, independentemente do pagamento.</summary>
    Bloqueado
}

public enum MotivoAcesso
{
    Permitido,
    ConsultaLiberadaPelaSecretaria,
    BloqueadoPelaSecretaria,
    MensalidadeEmAtraso,
    AlunoSemAcessoProprio
}

public readonly record struct DecisaoAcesso(bool Permitido, MotivoAcesso Motivo);

/// <summary>
/// Regras de quem pode consultar as notas. A consulta é sempre somente leitura.
/// Aluno: só a partir da 7.ª classe. Encarregado: depende da mensalidade,
/// salvo decisão manual da secretaria (Liberado / Bloqueado).
/// </summary>
public static class PoliticaAcessoNotas
{
    /// <param name="bloqueioPorMensalidadeAtivo">Definição da escola: se falso, a mensalidade não afeta o acesso.</param>
    public static DecisaoAcesso Avaliar(
        PerfilAcesso perfil,
        Classe classe,
        ModoAcessoNotas modo,
        bool mensalidadeEmDia,
        bool bloqueioPorMensalidadeAtivo)
    {
        if (perfil == PerfilAcesso.Aluno && !classe.AlunoTemAcessoProprio)
            return new(false, MotivoAcesso.AlunoSemAcessoProprio);

        return modo switch
        {
            ModoAcessoNotas.Bloqueado => new(false, MotivoAcesso.BloqueadoPelaSecretaria),
            ModoAcessoNotas.Liberado => new(true, MotivoAcesso.ConsultaLiberadaPelaSecretaria),
            _ when perfil == PerfilAcesso.Encarregado && bloqueioPorMensalidadeAtivo && !mensalidadeEmDia
                => new(false, MotivoAcesso.MensalidadeEmAtraso),
            _ => new(true, MotivoAcesso.Permitido)
        };
    }
}
