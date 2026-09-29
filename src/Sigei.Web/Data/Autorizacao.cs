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
    public const string AbrirFecharAnoLetivo = "Permissao:AbrirFecharAnoLetivo";
    public const string GerirDefinicoesEscola = "Permissao:GerirDefinicoesEscola";
    public const string DecidirAcessoNotas = "Permissao:DecidirAcessoNotas";
    public const string GerirMensalidades = "Permissao:GerirMensalidades";
    public const string LancarNotas = "Permissao:LancarNotas";
    public const string ConsultarNotas = "Permissao:ConsultarNotas";

    public static void Registar(AuthorizationOptions options)
    {
        foreach (var p in Enum.GetValues<Permissao>())
            options.AddPolicy(Nome(p), policy => policy.RequireAuthenticatedUser().AddRequirements(new PermissaoRequirement(p)));
    }
}

public sealed class PermissaoRequirement(Permissao permissao) : IAuthorizationRequirement
{
    public Permissao Permissao { get; } = permissao;
}

public sealed class PermissaoHandler(ApplicationDbContext db) : AuthorizationHandler<PermissaoRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissaoRequirement requirement)
    {
        if (!Enum.TryParse<PerfilUtilizador>(context.User.FindFirstValue(SigeiClaims.Perfil), out var perfil))
            return;
        if (!int.TryParse(context.User.FindFirstValue(SigeiClaims.EscolaId), out var escolaId))
            return; // sem escola, sem acesso aos dados pedagógicos

        // A permissão sobre o currículo depende de uma decisão da direção, guardada na escola.
        var secretariaGereDisciplinas = requirement.Permissao == Permissao.GerirCurriculoDisciplinas
            && perfil == PerfilUtilizador.Secretaria
            && await db.Escolas.Where(e => e.Id == escolaId).Select(e => e.SecretariaGereDisciplinas).FirstOrDefaultAsync();

        if (MatrizPermissoes.Tem(perfil, requirement.Permissao, secretariaGereDisciplinas))
            context.Succeed(requirement);
    }
}
