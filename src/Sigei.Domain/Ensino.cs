namespace Sigei.Domain;

public enum NivelEnsino
{
    Primario,      // Iniciação à 6.ª classe
    PrimeiroCiclo, // 7.ª à 9.ª classe
    SegundoCiclo   // 10.ª à 13.ª classe
}

/// <summary>Classe escolar. 0 representa a Iniciação; 1 a 13 são as classes seguintes.</summary>
public readonly record struct Classe
{
    public const int Iniciacao = 0;
    public const int Maxima = 13;
    public const int PrimeiraComAcessoProprio = 7;

    public int Numero { get; }

    public Classe(int numero)
    {
        if (numero is < Iniciacao or > Maxima)
            throw new ArgumentOutOfRangeException(nameof(numero), "A classe deve estar entre 0 (Iniciação) e 13.");
        Numero = numero;
    }

    public NivelEnsino Nivel => Numero switch
    {
        <= 6 => NivelEnsino.Primario,
        <= 9 => NivelEnsino.PrimeiroCiclo,
        _ => NivelEnsino.SegundoCiclo
    };

    /// <summary>A partir da 7.ª classe o aluno tem login próprio; antes disso o acesso é do encarregado.</summary>
    public bool AlunoTemAcessoProprio => Numero >= PrimeiraComAcessoProprio;

    public override string ToString() => Numero == Iniciacao ? "Iniciação" : $"{Numero}.ª classe";
}
