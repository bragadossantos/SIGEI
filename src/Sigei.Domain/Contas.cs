using System.Text.RegularExpressions;

namespace Sigei.Domain;

/// <summary>
/// Nomes de utilizador. Encarregados e alunos entram com o número de telefone; o pessoal da escola com um nome
/// de utilizador (ex.: "joana.secretaria"). Não se exige email, porque muitas famílias não o têm.
/// </summary>
public static partial class NomesDeUtilizador
{
    public const int Minimo = 3;
    public const int Maximo = 30;

    [GeneratedRegex(@"^\+?[\d\s\-()]{9,16}$")]
    private static partial Regex PareceTelefone();

    [GeneratedRegex(@"^[a-z0-9._\-]{3,30}$")]
    private static partial Regex UtilizadorValido();

    /// <summary>
    /// Telefones ficam só com os 9 dígitos nacionais (923 456 789, +244 923 456 789 e 00244923456789 dão o mesmo);
    /// nomes de utilizador ficam em minúsculas e sem espaços nas pontas.
    /// </summary>
    public static string Normalizar(string? texto)
    {
        var t = (texto ?? "").Trim();
        if (!PareceTelefone().IsMatch(t)) return t.ToLowerInvariant();

        var digitos = new string(t.Where(char.IsDigit).ToArray());
        if (digitos.StartsWith("00244") && digitos.Length == 14) digitos = digitos[5..];
        else if (digitos.StartsWith("244") && digitos.Length == 12) digitos = digitos[3..];
        return digitos;
    }

    public static bool EhTelefone(string normalizado) =>
        normalizado.Length == 9 && normalizado.All(char.IsDigit);

    /// <summary>Devolve o erro em português, ou null se o nome (já normalizado) for aceitável.</summary>
    public static string? Validar(string normalizado)
    {
        if (string.IsNullOrWhiteSpace(normalizado)) return "Indique o telefone ou o nome de utilizador.";
        if (EhTelefone(normalizado))
            return normalizado[0] == '9' ? null : "O telefone deve ter 9 dígitos e começar por 9.";
        if (normalizado.All(char.IsDigit)) return "O telefone deve ter 9 dígitos e começar por 9.";
        return UtilizadorValido().IsMatch(normalizado)
            ? null
            : $"O nome de utilizador deve ter entre {Minimo} e {Maximo} caracteres: letras minúsculas, números, ponto, hífen ou sublinhado.";
    }
}

/// <summary>Quem pode criar, repor a palavra-passe ou desativar contas de quem.</summary>
public static class GestaoDeContasRegras
{
    public static bool PodeGerir(PerfilUtilizador ator, PerfilUtilizador alvo) => ator switch
    {
        PerfilUtilizador.AdminSaas => alvo == PerfilUtilizador.Direcao,
        PerfilUtilizador.Direcao => alvo is PerfilUtilizador.Secretaria or PerfilUtilizador.Professor
                                         or PerfilUtilizador.Aluno or PerfilUtilizador.Encarregado,
        PerfilUtilizador.Secretaria => alvo is PerfilUtilizador.Aluno or PerfilUtilizador.Encarregado,
        _ => false
    };
}
