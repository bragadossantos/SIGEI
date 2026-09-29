using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sigei.Domain;

namespace Sigei.Web.Data;

/// <summary>Escola fictícia para testes locais. Só é criada em desenvolvimento e só se a base de dados estiver vazia.</summary>
public static class DadosDemonstracao
{
    public const string Palavrapasse = "Sigei@Demo2026";

    public static readonly (string Utilizador, string Nome, PerfilUtilizador Perfil)[] Contas =
    [
        ("direcao.demo", "Direção (demonstração)", PerfilUtilizador.Direcao),
        ("secretaria.demo", "Secretaria (demonstração)", PerfilUtilizador.Secretaria),
        ("professor.demo", "Professor (demonstração)", PerfilUtilizador.Professor),
        ("aluno8.demo", "Luzia Manuel", PerfilUtilizador.Aluno),
        ("923100001", "Maria Kiala", PerfilUtilizador.Encarregado),
    ];

    public static async Task CriarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await db.Database.MigrateAsync();
        if (await db.Escolas.AnyAsync()) return;

        var hoje = DateOnly.FromDateTime(DateTime.Today);

        var escola = new Escola { Nome = "Colégio Esperança (demonstração)", LicencaValidaAte = hoje.AddYears(1) };
        db.Escolas.Add(escola);
        await db.SaveChangesAsync();
        var e = escola.Id;

        var ids = new Dictionary<string, string>();
        foreach (var (utilizador, nome, perfil) in Contas)
        {
            var u = new ApplicationUser
            {
                UserName = utilizador, PhoneNumber = NomesDeUtilizador.EhTelefone(utilizador) ? utilizador : null,
                EscolaId = e, Perfil = perfil, NomeCompleto = nome
            };
            var r = await users.CreateAsync(u, Palavrapasse);
            if (!r.Succeeded) throw new InvalidOperationException(string.Join("; ", r.Errors.Select(x => x.Description)));
            ids[utilizador] = u.Id;
        }

        var ano = new AnoLetivo { EscolaId = e, Designacao = "2026/2027", Inicio = new(2026, 9, 1), Fim = new(2027, 7, 31) };
        var disciplinas = new[] { "Língua Portuguesa", "Matemática", "Ciências da Natureza", "História", "Educação Física" }
            .Select(n => new Disciplina { EscolaId = e, Nome = n }).ToList();
        db.AddRange(ano);
        db.AddRange(disciplinas);
        await db.SaveChangesAsync();

        var t5 = new Turma { EscolaId = e, AnoLetivoId = ano.Id, Nome = "5.ª A", ClasseNumero = 5, MensalidadeKz = 25000m };
        var t8 = new Turma { EscolaId = e, AnoLetivoId = ano.Id, Nome = "8.ª B", ClasseNumero = 8, MensalidadeKz = 35000m };
        db.AddRange(t5, t8);
        await db.SaveChangesAsync();

        foreach (var t in new[] { t5, t8 })
            foreach (var d in disciplinas)
                db.TurmasDisciplinas.Add(new TurmaDisciplina
                {
                    EscolaId = e, TurmaId = t.Id, DisciplinaId = d.Id,
                    ProfessorUserId = ids["professor.demo"]
                });

        // (aluno, encarregado, turma, mensalidade em atraso?, modo)
        var casos = new (string Aluno, int Idade, string Enc, string Tel, Turma Turma, bool Atraso, ModoAcessoNotas Modo, string? AlunoUser, string? EncUser)[]
        {
            ("Mateus Kiala",       11, "Maria Kiala",       "923 100 001", t5, false, ModoAcessoNotas.Automatico, null, "923100001"),
            ("Ndalu Fernandes",    10, "António Fernandes", "923 100 002", t5, true,  ModoAcessoNotas.Automatico, null, null),
            ("Sofia Bento",        11, "Rosa Bento",        "923 100 003", t5, true,  ModoAcessoNotas.Liberado,   null, null),
            ("Luzia Manuel",       14, "Paulo Manuel",      "923 100 004", t8, false, ModoAcessoNotas.Automatico, "aluno8.demo", null),
            ("Domingos Cassoma",   14, "Joana Cassoma",     "923 100 005", t8, true,  ModoAcessoNotas.Automatico, null, null),
            ("Esperança Paulo",    15, "Filipe Paulo",      "923 100 006", t8, true,  ModoAcessoNotas.Bloqueado,  null, null),
        };

        foreach (var c in casos)
        {
            var aluno = new Aluno
            {
                EscolaId = e, Nome = c.Aluno, DataNascimento = hoje.AddYears(-c.Idade),
                UserId = c.AlunoUser is null ? null : ids[c.AlunoUser]
            };
            var enc = new Encarregado
            {
                EscolaId = e, Nome = c.Enc, Telefone = c.Tel,
                UserId = c.EncUser is null ? null : ids[c.EncUser]
            };
            aluno.Encarregados.Add(enc);
            db.Add(aluno);
            await db.SaveChangesAsync();

            var valor = c.Turma.ClasseNumero >= 7 ? 35000m : 25000m;
            var mat = new Matricula
            {
                EscolaId = e, AnoLetivoId = ano.Id, AlunoId = aluno.Id, TurmaId = c.Turma.Id,
                ClasseNumero = c.Turma.ClasseNumero, ModoAcessoNotas = c.Modo
            };
            mat.Mensalidades.Add(new Mensalidade
            {
                EscolaId = e, Referencia = "2026-09", ValorKz = valor, Vencimento = new(2026, 9, 5),
                PagaEm = c.Atraso ? null : new(2026, 9, 3)
            });
            mat.Mensalidades.Add(new Mensalidade
            {
                EscolaId = e, Referencia = "2026-10", ValorKz = valor, Vencimento = new(2026, 10, 5)
            });
            // Documentos: o primeiro caso fica completo; os outros ficam a meio (para mostrar a checklist).
            var exigidos = Documentos.Exigidos(mat.Classe);
            for (var i = 0; i < exigidos.Count; i++)
                mat.Documentos.Add(new DocumentoMatricula
                {
                    EscolaId = e, Tipo = exigidos[i],
                    EntregueEm = c.Aluno == "Mateus Kiala" || i == 0 ? new DateOnly(2026, 8, 20) : null
                });
            db.Add(mat);
            await db.SaveChangesAsync();

            // Notas do 1.º trimestre para o aluno poder ver algo.
            var rnd = new Random(aluno.Id);
            foreach (var td in db.TurmasDisciplinas.IgnoreQueryFilters().Where(x => x.TurmaId == c.Turma.Id))
                db.Notas.Add(new Nota
                {
                    EscolaId = e, MatriculaId = mat.Id, TurmaDisciplinaId = td.Id, Trimestre = Trimestre.Primeiro,
                    Mac = rnd.Next(8, 18), Npp = rnd.Next(8, 18), Npt = rnd.Next(8, 18)
                });
            await db.SaveChangesAsync();
        }
    }
}
