namespace Sigei.Domain;

public enum TipoDocumento
{
    BilheteOuCedula,
    Fotografias,
    BoletimVacinas,
    CertificadoAnterior
}

public static class Documentos
{
    public static string Nome(TipoDocumento t) => t switch
    {
        TipoDocumento.BilheteOuCedula => "Bilhete de identidade ou cédula",
        TipoDocumento.Fotografias => "Fotografias tipo passe",
        TipoDocumento.BoletimVacinas => "Boletim de vacinas",
        TipoDocumento.CertificadoAnterior => "Certificado ou declaração do ano anterior",
        _ => t.ToString()
    };

    /// <summary>
    /// Documentos exigidos numa matrícula. Ponto de partida geral: cada escola deve confirmar
    /// a sua lista com as exigências do Ministério da Educação e da sua direção provincial.
    /// </summary>
    public static IReadOnlyList<TipoDocumento> Exigidos(Classe classe) =>
        classe.Numero == Classe.Iniciacao
            ? [TipoDocumento.BilheteOuCedula, TipoDocumento.Fotografias, TipoDocumento.BoletimVacinas]
            : [TipoDocumento.BilheteOuCedula, TipoDocumento.Fotografias, TipoDocumento.CertificadoAnterior];
}

public readonly record struct PrestacaoPlaneada(string Referencia, DateOnly Vencimento);

public static class PlanoMensalidades
{
    public const int DiaDeVencimento = 5;

    /// <summary>Uma prestação por mês, a começar no mês de início do ano letivo, a vencer no dia 5.</summary>
    public static IReadOnlyList<PrestacaoPlaneada> Gerar(DateOnly inicioAno, int numero)
    {
        var primeiro = new DateOnly(inicioAno.Year, inicioAno.Month, 1);
        return Enumerable.Range(0, Math.Max(numero, 0))
            .Select(i => primeiro.AddMonths(i))
            .Select(m => new PrestacaoPlaneada($"{m:yyyy-MM}", new DateOnly(m.Year, m.Month, DiaDeVencimento)))
            .ToList();
    }
}

public static class RegrasAnoLetivo
{
    /// <summary>Devolve o erro em português, ou null se o ano for válido.</summary>
    public static string? Validar(string? designacao, DateOnly inicio, DateOnly fim)
    {
        if (string.IsNullOrWhiteSpace(designacao)) return "Indique a designação do ano letivo (por exemplo 2026/2027).";
        if (fim <= inicio) return "A data de fim tem de ser posterior à data de início.";
        if (fim.DayNumber - inicio.DayNumber < 120) return "Um ano letivo tem de durar pelo menos 4 meses.";
        return null;
    }
}
