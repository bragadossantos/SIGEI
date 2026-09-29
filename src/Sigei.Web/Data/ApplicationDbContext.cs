using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sigei.Domain;

namespace Sigei.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantAccessor tenant)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Escola> Escolas => Set<Escola>();
    public DbSet<AnoLetivo> AnosLetivos => Set<AnoLetivo>();
    public DbSet<Disciplina> Disciplinas => Set<Disciplina>();
    public DbSet<Turma> Turmas => Set<Turma>();
    public DbSet<TurmaDisciplina> TurmasDisciplinas => Set<TurmaDisciplina>();
    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<Encarregado> Encarregados => Set<Encarregado>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
    public DbSet<Mensalidade> Mensalidades => Set<Mensalidade>();
    public DbSet<Nota> Notas => Set<Nota>();
    public DbSet<Anuncio> Anuncios => Set<Anuncio>();
    public DbSet<DocumentoMatricula> DocumentosMatricula => Set<DocumentoMatricula>();
    public DbSet<RegistoAlteracao> Alteracoes => Set<RegistoAlteracao>();

    // Lido a cada consulta pelo filtro global; sem escola identificada não devolve nada.
    public int? EscolaAtual => tenant.EscolaId;

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<ApplicationUser>().Property(u => u.Perfil).HasConversion<string>().HasMaxLength(20);

        b.Entity<Escola>().HasIndex(e => e.Nome);
        b.Entity<Escola>().Property(e => e.Plano).HasConversion<string>().HasMaxLength(12);
        b.Entity<AnoLetivo>().HasIndex(a => new { a.EscolaId, a.Designacao }).IsUnique();
        b.Entity<Disciplina>().HasIndex(d => new { d.EscolaId, d.Nome }).IsUnique();
        b.Entity<Turma>().HasIndex(t => new { t.AnoLetivoId, t.Nome }).IsUnique();
        b.Entity<TurmaDisciplina>().HasIndex(t => new { t.TurmaId, t.DisciplinaId }).IsUnique();
        b.Entity<Matricula>().HasIndex(m => new { m.AnoLetivoId, m.AlunoId }).IsUnique(); // 1 matrícula por aluno e ano
        b.Entity<Matricula>().Property(m => m.ModoAcessoNotas).HasConversion<string>().HasMaxLength(12);
        b.Entity<Matricula>().Ignore(m => m.Classe);
        b.Entity<Mensalidade>().Property(m => m.ValorKz).HasPrecision(14, 2);
        b.Entity<Mensalidade>().HasIndex(m => new { m.MatriculaId, m.Referencia }).IsUnique();
        b.Entity<Turma>().Property(t => t.MensalidadeKz).HasPrecision(14, 2);
        b.Entity<DocumentoMatricula>().Property(d => d.Tipo).HasConversion<string>().HasMaxLength(24);
        b.Entity<DocumentoMatricula>().HasIndex(d => new { d.MatriculaId, d.Tipo }).IsUnique();
        b.Entity<Anuncio>().Property(a => a.Titulo).HasMaxLength(Anuncio.TituloMaximo);
        b.Entity<Anuncio>().Property(a => a.Texto).HasMaxLength(Anuncio.TextoMaximo);
        b.Entity<Anuncio>().Property(a => a.Destinatarios).HasConversion<string>().HasMaxLength(20);
        b.Entity<Anuncio>().HasIndex(a => new { a.EscolaId, a.Arquivado, a.PublicadoEmUtc });
        b.Entity<Nota>().HasIndex(n => new { n.MatriculaId, n.TurmaDisciplinaId, n.Trimestre }).IsUnique();
        b.Entity<Nota>().Ignore(n => n.Valores);
        b.Entity<Nota>().Property(n => n.Trimestre).HasConversion<int>();
        foreach (var col in new[] { "Mac", "Npp", "Npt" })
            b.Entity<Nota>().Property<decimal?>(col).HasPrecision(4, 1);
        b.Entity<Aluno>().HasMany(a => a.Encarregados).WithMany(e => e.Educandos);
        b.Entity<RegistoAlteracao>().HasIndex(r => new { r.EscolaId, r.QuandoUtc });

        // Isolamento entre escolas: cada consulta só vê a sua escola.
        foreach (var tipo in b.Model.GetEntityTypes().Where(t => typeof(IPertenceEscola).IsAssignableFrom(t.ClrType)))
        {
            var p = Expression.Parameter(tipo.ClrType, "e");
            var escolaDoRegisto = Expression.Convert(Expression.Property(p, nameof(IPertenceEscola.EscolaId)), typeof(int?));
            var escolaAtual = Expression.Property(Expression.Constant(this), nameof(EscolaAtual));
            var filtro = Expression.Lambda(Expression.Equal(escolaDoRegisto, escolaAtual), p);
            tipo.SetQueryFilter(filtro);
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        base.SaveChanges(AtribuirEscola(acceptAllChangesOnSuccess));

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default) =>
        base.SaveChangesAsync(AtribuirEscola(acceptAllChangesOnSuccess), ct);

    /// <summary>Novos registos ficam sempre na escola do utilizador; nunca se aceita uma escola vinda de fora.</summary>
    private bool AtribuirEscola(bool valor)
    {
        foreach (var e in ChangeTracker.Entries<IPertenceEscola>())
        {
            if (e.State == EntityState.Added && tenant.EscolaId is { } escola)
                e.Entity.EscolaId = escola;
            else if (e.State == EntityState.Modified && e.Property(x => x.EscolaId).IsModified)
                throw new InvalidOperationException("Não é permitido mudar um registo de escola.");
        }
        return valor;
    }
}
