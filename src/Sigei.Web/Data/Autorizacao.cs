using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sigei.Domain;

namespace Sigei.Web.Data;

public static class Politicas
{
    public static string Nome(Permissao p) => $"Permissao:{p}";

    public const string GerirCurriculoDisciplinas = "Permissao:GerirCurriculoDisciplinas";
    public const string AssociarDisciplinasATurmas = "Permissao:AssociarDisciplinasATurmas";
    public const string GerirMatriculasETurmas = "Permissao:GerirMatriculasETurmas";
    public const string GerirDocumentosEAvisos = "Permissao:GerirDocumentosEAvisos";
    public const string AbrirFecharAnoLetivo = "Permissao:AbrirFecharAnoLetivo";
    public const string GerirDefinicoesEscola = "Permissao:GerirDefinicoesEscola";
    public const string AdministrarPlataforma = "Permissao:AdministrarPlataforma";
    public const string DecidirAcessoNotas = "Permissao:DecidirAcessoNotas";
    public const string GerirMensalidades = "Permissao:GerirMensalidades";
    public const string LancarNotas = "Permissao:LancarNotas";
    public const string ConsultarNotas = "Permissao:ConsultarNotas";

    /// <summary>Qualquer utilizador de uma escola com licença utilizável (para ver anúncios, por exemplo).</summary>
    public const string MembroDaEscola = "MembroDaEscola";

    public static void Registar(AuthorizationOptions options)
    {
        foreach (var p in Enum.GetValues<Permissao>())
            options.AddPolicy(Nome(p), policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissaoRequirement(p)));
        options.AddPolicy(MembroDaEscola, policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissaoRequirement(null)));
    }
}

/// <param name="permissao">Nulo = basta ser membro de uma escola com licença utilizável.</param>
public sealed class PermissaoRequirement(Permissao? permissao) : IAuthorizationRequirement
{
    public Permissao? Permissao { get; } = permissao;
}

public sealed class PermissaoHandler(ApplicationDbContext db) : AuthorizationHandler<PermissaoRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissaoRequirement requirement)
    {
        if (!Enum.TryParse<PerfilUtilizador>(context.User.FindFirstValue(SigeiClaims.Perfil), out var perfil))
            return;

        // Enquanto tiver de mudar a palavra-passe temporária, a pessoa não acede a mais nada.
        if (context.User.HasClaim(c => c.Type == SigeiClaims.MudarPalavrapasse))
            return;

        if (perfil == PerfilUtilizador.AdminSaas)
        {
            if (requirement.Permissao is { } p && MatrizPermissoes.Tem(perfil, p))
                context.Succeed(requirement);
            return;
        }

        if (!int.TryParse(context.User.FindFirstValue(SigeiClaims.EscolaId), out var escolaId))
            return; // sem escola, sem acesso aos dados pedagógicos

        var escola = await db.Escolas.AsNoTracking()
            .Where(e => e.Id == escolaId)
            .Select(e => new { e.Ativa, e.LicencaValidaAte, e.SecretariaGereDisciplinas })
            .FirstOrDefaultAsync();
        if (escola is null) return;

        // Licença expirada (fora da carência) ou escola suspensa: ninguém da escola entra.
        var estado = Escola.CalcularEstado(escola.Ativa, escola.LicencaValidaAte, DateOnly.FromDateTime(DateTime.Today));
        if (estado is EstadoLicenca.Expirada or EstadoLicenca.Suspensa) return;

        if (requirement.Permissao is not { } permissao)
        {
            context.Succeed(requirement);
            return;
        }

        var secretariaGereDisciplinas = permissao == Permissao.GerirCurriculoDisciplinas
            && perfil == PerfilUtilizador.Secretaria && escola.SecretariaGereDisciplinas;

        if (MatrizPermissoes.Tem(perfil, permissao, secretariaGereDisciplinas))
            context.Succeed(requirement);
    }
}
