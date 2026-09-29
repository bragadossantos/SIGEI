using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Sigei.Domain;

namespace Sigei.Web.Data;

public readonly record struct Ator(string UserId, PerfilUtilizador Perfil, int? EscolaId);

public sealed record ResultadoConta(bool Sucesso, string? Erro, string? UserId = null, string? Utilizador = null, string? Palavrapasse = null)
{
    public static ResultadoConta Falha(string erro) => new(false, erro);
}

/// <summary>
/// Único sítio onde se criam contas e se repõem palavras-passe. O perfil e a escola vêm sempre de quem
/// chama (o servidor), nunca de um formulário, e cada ação é validada contra quem a pede.
/// </summary>
public sealed class GestaoContas(UserManager<ApplicationUser> utilizadores, ApplicationDbContext db)
{
    public async Task<ResultadoConta> CriarAsync(Ator ator, string utilizador, string nome, PerfilUtilizador perfil, int escolaId)
    {
        if (!GestaoDeContasRegras.PodeGerir(ator.Perfil, perfil))
            return ResultadoConta.Falha("Não tem permissão para criar este tipo de conta.");
        if (ator.Perfil != PerfilUtilizador.AdminSaas && ator.EscolaId != escolaId)
            return ResultadoConta.Falha("Não pode criar contas noutra escola.");

        var normalizado = NomesDeUtilizador.Normalizar(utilizador);
        if (NomesDeUtilizador.Validar(normalizado) is { } erro) return ResultadoConta.Falha(erro);
        if (await utilizadores.FindByNameAsync(normalizado) is not null)
            return ResultadoConta.Falha("Já existe uma conta com esse telefone ou nome de utilizador.");

        var palavrapasse = GerarPalavrapasse();
        var u = new ApplicationUser
        {
            UserName = normalizado, EscolaId = escolaId, Perfil = perfil, NomeCompleto = nome.Trim(),
            PhoneNumber = NomesDeUtilizador.EhTelefone(normalizado) ? normalizado : null,
            MudarPalavrapasse = true
        };
        var r = await utilizadores.CreateAsync(u, palavrapasse);
        if (!r.Succeeded) return ResultadoConta.Falha(string.Join(" ", r.Errors.Select(e => e.Description)));

        await RegistarAsync(ator, escolaId, u.Id, "ContaCriada", null, $"{normalizado} ({perfil})");
        return new(true, null, u.Id, normalizado, palavrapasse);
    }

    public async Task<ResultadoConta> ReporPalavrapasseAsync(Ator ator, string userId)
    {
        var (alvo, erro) = await ObterGeriveisAsync(ator, userId);
        if (alvo is null) return ResultadoConta.Falha(erro!);

        var palavrapasse = GerarPalavrapasse();
        var token = await utilizadores.GeneratePasswordResetTokenAsync(alvo);
        var r = await utilizadores.ResetPasswordAsync(alvo, token, palavrapasse);
        if (!r.Succeeded) return ResultadoConta.Falha(string.Join(" ", r.Errors.Select(e => e.Description)));

        alvo.MudarPalavrapasse = true;
        await utilizadores.SetLockoutEndDateAsync(alvo, null);   // levanta um bloqueio por tentativas falhadas
        await utilizadores.ResetAccessFailedCountAsync(alvo);
        await utilizadores.UpdateAsync(alvo);
        await RegistarAsync(ator, alvo.EscolaId ?? 0, alvo.Id, "PalavrapasseRepostaPorTerceiro", null, alvo.UserName);
        return new(true, null, alvo.Id, alvo.UserName, palavrapasse);
    }

    public async Task<ResultadoConta> AlterarEstadoAsync(Ator ator, string userId, bool ativa)
    {
        var (alvo, erro) = await ObterGeriveisAsync(ator, userId);
        if (alvo is null) return ResultadoConta.Falha(erro!);

        await utilizadores.SetLockoutEnabledAsync(alvo, true);
        await utilizadores.SetLockoutEndDateAsync(alvo, ativa ? null : DateTimeOffset.MaxValue);
        await utilizadores.UpdateSecurityStampAsync(alvo); // termina as sessões abertas
        await RegistarAsync(ator, alvo.EscolaId ?? 0, alvo.Id, ativa ? "ContaReativada" : "ContaDesativada", null, alvo.UserName);
        return new(true, null, alvo.Id, alvo.UserName);
    }

    public static bool EstaDesativada(ApplicationUser u) =>
        u.LockoutEnd is { } fim && fim > DateTimeOffset.UtcNow.AddYears(50);

    private async Task<(ApplicationUser? Alvo, string? Erro)> ObterGeriveisAsync(Ator ator, string userId)
    {
        // Os utilizadores não têm filtro automático por escola: a escola confere-se aqui.
        var alvo = await utilizadores.FindByIdAsync(userId);
        if (alvo is null) return (null, "Conta não encontrada.");
        if (alvo.Id == ator.UserId) return (null, "Não pode alterar a sua própria conta aqui.");
        if (!GestaoDeContasRegras.PodeGerir(ator.Perfil, alvo.Perfil)) return (null, "Não tem permissão para gerir esta conta.");
        if (ator.Perfil != PerfilUtilizador.AdminSaas && alvo.EscolaId != ator.EscolaId) return (null, "Conta não encontrada.");
        return (alvo, null);
    }

    private async Task RegistarAsync(Ator ator, int escolaId, string alvoId, string acao, string? antes, string? depois)
    {
        db.Alteracoes.Add(new RegistoAlteracao
        {
            EscolaId = escolaId, UserId = ator.UserId, Entidade = nameof(ApplicationUser), EntidadeId = alvoId,
            Acao = acao, ValorAnterior = antes, ValorNovo = depois
        });
        await db.SaveChangesAsync();
    }

    public static string GerarPalavrapasse()
    {
        const string maiusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ", minusculas = "abcdefghijkmnpqrstuvwxyz", digitos = "23456789", especiais = "@#$%";
        static char Um(string s) => s[RandomNumberGenerator.GetInt32(s.Length)];
        var chars = new List<char> { Um(maiusculas), Um(minusculas), Um(digitos), Um(especiais) };
        chars.AddRange(Enumerable.Range(0, 6).Select(_ => Um(maiusculas + minusculas + digitos)));
        RandomNumberGenerator.Shuffle(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(chars));
        return new string(chars.ToArray());
    }
}
