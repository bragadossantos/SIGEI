namespace Sigei.Domain;

/// <summary>Entidade que pertence a uma escola (tenant). Todos os dados pedagógicos são isolados por EscolaId.</summary>
public interface IPertenceEscola
{
    int EscolaId { get; set; }
}

public enum EstadoLicenca { Valida, SoLeitura, Expirada, Suspensa }

public enum PlanoLicenca { Teste, Pequena, Media, Grande }

public class Escola
{
    public int Id { get; set; }
    public required string Nome { get; set; }
    public DateOnly LicencaValidaAte { get; set; }
    public bool Ativa { get; set; } = true;
    public PlanoLicenca Plano { get; set; } = PlanoLicenca.Teste;

    /// <summary>Número de alunos incluído no plano (0 = sem limite). Ultrapassar avisa, não bloqueia matrículas.</summary>
    public int LimiteAlunos { get; set; }
    public bool BloqueioPorMensalidadeAtivo { get; set; } = true;
    public bool SecretariaGereDisciplinas { get; set; }
    public int DiasToleranciaMensalidade { get; set; } = 5;
    public int NumeroMensalidades { get; set; } = 10; // Setembro a Junho

    /// <summary>Licença expirada = só leitura durante a carência (30 dias); depois disso, sem acesso.</summary>
    public EstadoLicenca EstadoLicenca(DateOnly hoje) => CalcularEstado(Ativa, LicencaValidaAte, hoje);

    public const int DiasDeCarencia = 30;

    /// <summary>
    /// Nova data de validade ao renovar por N meses: se a licença ainda está válida, soma-se ao fim atual
    /// (a escola não perde dias pagos); se já expirou, conta a partir de hoje.
    /// </summary>
    public static DateOnly RenovarValidade(DateOnly validaAte, DateOnly hoje, int meses) =>
        (validaAte >= hoje ? validaAte : hoje).AddMonths(meses);


    public static EstadoLicenca CalcularEstado(bool ativa, DateOnly validaAte, DateOnly hoje) =>
        !ativa ? Domain.EstadoLicenca.Suspensa
        : hoje <= validaAte ? Domain.EstadoLicenca.Valida
        : hoje <= validaAte.AddDays(DiasDeCarencia) ? Domain.EstadoLicenca.SoLeitura
        : Domain.EstadoLicenca.Expirada;
}

public enum EstadoAnoLetivo { Aberto, Fechado }

public class AnoLetivo : IPertenceEscola
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public required string Designacao { get; set; } // ex.: "2026/2027"
    public DateOnly Inicio { get; set; }
    public DateOnly Fim { get; set; }
    public EstadoAnoLetivo Estado { get; set; } = EstadoAnoLetivo.Aberto;
    public DateTime? BackupFeitoEm { get; set; }
}

/// <summary>Disciplina do currículo. Nunca se elimina: desativa-se, para preservar o histórico de notas.</summary>
public class Disciplina : IPertenceEscola
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public required string Nome { get; set; }
    public bool Ativa { get; set; } = true;
}

public class Turma : IPertenceEscola
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public int AnoLetivoId { get; set; }
    public required string Nome { get; set; } // ex.: "7.ª A"
    public int ClasseNumero { get; set; }
    public decimal MensalidadeKz { get; set; }
}

public class TurmaDisciplina : IPertenceEscola
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public int TurmaId { get; set; }
    public int DisciplinaId { get; set; }
    public string? ProfessorUserId { get; set; }
}

public class Aluno : IPertenceEscola
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public required string Nome { get; set; }
    public DateOnly DataNascimento { get; set; }
    public string? UserId { get; set; } // login próprio, só a partir da 7.ª classe
    public List<Encarregado> Encarregados { get; set; } = [];
}

public class Encarregado : IPertenceEscola
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public required string Nome { get; set; }
    public string? Telefone { get; set; }
    public string? UserId { get; set; }
    public List<Aluno> Educandos { get; set; } = [];
}

public class Matricula : IPertenceEscola
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public int AnoLetivoId { get; set; }
    public int AlunoId { get; set; }
    public int TurmaId { get; set; }
    public int ClasseNumero { get; set; }
    public ModoAcessoNotas ModoAcessoNotas { get; set; } = ModoAcessoNotas.Automatico;
    public Aluno? Aluno { get; set; }
    public Turma? Turma { get; set; }
    public List<Mensalidade> Mensalidades { get; set; } = [];
    public List<DocumentoMatricula> Documentos { get; set; } = [];

    public Classe Classe => new(ClasseNumero);

    public DecisaoAcesso PodeVerNotas(PerfilAcesso perfil, Escola escola, DateOnly hoje) =>
        PoliticaAcessoNotas.Avaliar(perfil, Classe, ModoAcessoNotas,
            Mensalidade.EmDia(Mensalidades, hoje, escola.DiasToleranciaMensalidade),
            escola.BloqueioPorMensalidadeAtivo);
}

public class Mensalidade : IPertenceEscola
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public int MatriculaId { get; set; }
    public required string Referencia { get; set; } // ex.: "2026-10"
    public decimal ValorKz { get; set; }
    public DateOnly Vencimento { get; set; }
    public DateOnly? PagaEm { get; set; }

    /// <summary>Em dia = nenhuma mensalidade por pagar com vencimento anterior a (hoje − tolerância).</summary>
    public static bool EmDia(IEnumerable<Mensalidade> mensalidades, DateOnly hoje, int diasTolerancia) =>
        !mensalidades.Any(m => m.PagaEm is null && m.Vencimento.AddDays(diasTolerancia) < hoje);
}

public enum Trimestre { Primeiro = 1, Segundo = 2, Terceiro = 3 }

public class Nota : IPertenceEscola
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public int MatriculaId { get; set; }
    public int TurmaDisciplinaId { get; set; }
    public Trimestre Trimestre { get; set; }
    public decimal? Mac { get; set; }
    public decimal? Npp { get; set; }
    public decimal? Npt { get; set; }

    public NotasTrimestre Valores => new(Mac, Npp, Npt);
}

/// <summary>Quem alterou o quê, quando, e o valor anterior.</summary>
public class RegistoAlteracao : IPertenceEscola
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public required string UserId { get; set; }
    public required string Entidade { get; set; }
    public required string EntidadeId { get; set; }
    public required string Acao { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNovo { get; set; }
    public DateTime QuandoUtc { get; set; } = DateTime.UtcNow;
}

public class DocumentoMatricula : IPertenceEscola
{
    public int Id { get; set; }
    public int EscolaId { get; set; }
    public int MatriculaId { get; set; }
    public TipoDocumento Tipo { get; set; }
    public DateOnly? EntregueEm { get; set; }
    public string? Observacao { get; set; }
}
