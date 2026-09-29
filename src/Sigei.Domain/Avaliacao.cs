namespace Sigei.Domain;

/// <summary>
/// Notas de um trimestre numa disciplina (escala 0–20).
/// MAC = avaliação contínua, NPP = prova do professor, NPT = prova trimestral.
/// </summary>
public sealed record NotasTrimestre(decimal? Mac, decimal? Npp, decimal? Npt)
{
    public const decimal NotaMinimaPositiva = 10m;

    /// <summary>MT = (MAC + NPP + NPT) / 3. Só existe quando as três notas foram lançadas.</summary>
    public decimal? Mt =>
        Mac is { } mac && Npp is { } npp && Npt is { } npt
            ? Arredondar((mac + npp + npt) / 3m)
            : null;

    public bool? Positiva => Mt is { } mt ? mt >= NotaMinimaPositiva : null;

    /// <summary>Média final da disciplina: média aritmética dos MT dos três trimestres.</summary>
    public static decimal? MediaFinal(NotasTrimestre t1, NotasTrimestre t2, NotasTrimestre t3) =>
        t1.Mt is { } a && t2.Mt is { } b && t3.Mt is { } c
            ? Arredondar((a + b + c) / 3m)
            : null;

    private static decimal Arredondar(decimal valor) =>
        Math.Round(valor, 0, MidpointRounding.AwayFromZero);
}
