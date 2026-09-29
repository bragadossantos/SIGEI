using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sigei.Domain;

namespace Sigei.Web.Data;

public sealed class BackupOptions
{
    /// <summary>Pasta dos backups, fora de wwwroot (os ficheiros contêm dados pessoais de menores).</summary>
    public string Pasta { get; set; } = "App_Data/backups";
}

public sealed record ResultadoBackup(string Ficheiro, string CaminhoCompleto, string Sha256, int Alunos, int Notas);

/// <summary>
/// Exporta para JSON todos os dados de uma escola relativos a um ano letivo. O contexto já filtra por escola,
/// por isso o ficheiro nunca contém dados de outra escola.
/// </summary>
public sealed class BackupAnoLetivoService(ApplicationDbContext db, IOptions<BackupOptions> opcoes)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string NomeSeguro(string texto) =>
        new(texto.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());

    public async Task<ResultadoBackup> CriarAsync(int escolaId, int anoLetivoId, CancellationToken ct = default)
    {
        if (db.EscolaAtual != escolaId)
            throw new InvalidOperationException("A escola do pedido não coincide com a escola do backup.");

        var escola = await db.Escolas.AsNoTracking().FirstAsync(e => e.Id == escolaId, ct);
        var ano = await db.AnosLetivos.AsNoTracking().FirstAsync(a => a.Id == anoLetivoId, ct);

        var turmas = await db.Turmas.AsNoTracking().Where(t => t.AnoLetivoId == ano.Id).ToListAsync(ct);
        var turmaIds = turmas.Select(t => t.Id).ToList();
        var matriculas = await db.Matriculas.AsNoTracking().Where(m => m.AnoLetivoId == ano.Id).ToListAsync(ct);
        var matriculaIds = matriculas.Select(m => m.Id).ToList();
        var alunoIds = matriculas.Select(m => m.AlunoId).Distinct().ToList();

        var alunos = await db.Alunos.AsNoTracking().Include(a => a.Encarregados).Where(a => alunoIds.Contains(a.Id)).AsSplitQuery().ToListAsync(ct);
        var turmaDisciplinas = await db.TurmasDisciplinas.AsNoTracking().Where(td => turmaIds.Contains(td.TurmaId)).ToListAsync(ct);
        var disciplinas = await db.Disciplinas.AsNoTracking().ToListAsync(ct);
        var mensalidades = await db.Mensalidades.AsNoTracking().Where(m => matriculaIds.Contains(m.MatriculaId)).ToListAsync(ct);
        var documentos = await db.DocumentosMatricula.AsNoTracking().Where(d => matriculaIds.Contains(d.MatriculaId)).ToListAsync(ct);
        var notas = await db.Notas.AsNoTracking().Where(n => matriculaIds.Contains(n.MatriculaId)).ToListAsync(ct);

        var conteudo = new
        {
            versao = 1,
            geradoEmUtc = DateTime.UtcNow,
            escola = new { escola.Id, escola.Nome },
            anoLetivo = new { ano.Id, ano.Designacao, ano.Inicio, ano.Fim, estado = ano.Estado.ToString() },
            disciplinas = disciplinas.Select(d => new { d.Id, d.Nome, d.Ativa }),
            turmas = turmas.Select(t => new { t.Id, t.Nome, t.ClasseNumero, t.MensalidadeKz }),
            turmaDisciplinas = turmaDisciplinas.Select(td => new { td.Id, td.TurmaId, td.DisciplinaId, td.ProfessorUserId }),
            alunos = alunos.Select(a => new
            {
                a.Id, a.Nome, a.DataNascimento,
                encarregados = a.Encarregados.Select(e => new { e.Id, e.Nome, e.Telefone })
            }),
            matriculas = matriculas.Select(m => new { m.Id, m.AlunoId, m.TurmaId, m.ClasseNumero, modoAcessoNotas = m.ModoAcessoNotas.ToString() }),
            mensalidades = mensalidades.Select(m => new { m.Id, m.MatriculaId, m.Referencia, m.ValorKz, m.Vencimento, m.PagaEm }),
            documentos = documentos.Select(d => new { d.MatriculaId, tipo = d.Tipo.ToString(), d.EntregueEm, d.Observacao }),
            notas = notas.Select(n => new { n.MatriculaId, n.TurmaDisciplinaId, trimestre = (int)n.Trimestre, n.Mac, n.Npp, n.Npt })
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(conteudo, Json);

        var pasta = Path.GetFullPath(opcoes.Value.Pasta);
        Directory.CreateDirectory(pasta);
        var nome = $"escola-{escolaId}_{NomeSeguro(ano.Designacao)}_{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        var caminho = Path.Combine(pasta, nome);

        // Escreve num ficheiro temporário e só depois o publica, para nunca deixar um backup a meio.
        var temporario = caminho + ".tmp";
        await File.WriteAllBytesAsync(temporario, bytes, ct);
        var sha = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        File.Move(temporario, caminho);
        await File.WriteAllTextAsync(caminho + ".sha256", $"{sha}  {nome}\n", Encoding.UTF8, ct);

        return new ResultadoBackup(nome, caminho, sha, alunos.Count, notas.Count);
    }

    /// <summary>Um ficheiro só pode ser descarregado pela escola a que pertence.</summary>
    public static bool PertenceAEscola(string ficheiro, int escolaId) =>
        ficheiro == Path.GetFileName(ficheiro)
        && ficheiro.StartsWith($"escola-{escolaId}_", StringComparison.Ordinal)
        && ficheiro.EndsWith(".json", StringComparison.Ordinal);
}
