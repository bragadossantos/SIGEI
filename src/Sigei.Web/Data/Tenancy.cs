using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Sigei.Domain;

namespace Sigei.Web.Data;

public static class SigeiClaims
{
    public const string EscolaId = "sigei:escola_id";
    public const string Perfil = "sigei:perfil";
    public const string MudarPalavrapasse = "sigei:mudar_senha";
}

/// <summary>Identifica a escola do pedido atual. Sem escola identificada, as consultas não devolvem dados (falha fechada).</summary>
public interface ITenantAccessor
{
    int? EscolaId { get; }
}

/// <summary>
/// Nos pedidos normais lê a escola do cookie de sessão. Nas páginas interativas (Blazor Server) não há
/// HttpContext, por isso a página define a escola a partir do estado de autenticação.
/// </summary>
public sealed class ScopedTenantAccessor(IHttpContextAccessor http) : ITenantAccessor
{
    private int? _definida;

    public int? EscolaId =>
        _definida ?? (int.TryParse(http.HttpContext?.User.FindFirstValue(SigeiClaims.EscolaId), out var id) ? id : null);

    public void Definir(int? escolaId) => _definida = escolaId;
}

/// <summary>Acrescenta escola e perfil ao cookie de sessão.</summary>
public sealed class SigeiClaimsFactory(UserManager<ApplicationUser> users, IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser>(users, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(SigeiClaims.Perfil, user.Perfil.ToString()));
        if (user.MudarPalavrapasse)
            identity.AddClaim(new Claim(SigeiClaims.MudarPalavrapasse, "1"));
        if (user.EscolaId is { } escola)
            identity.AddClaim(new Claim(SigeiClaims.EscolaId, escola.ToString()));
        return identity;
    }
}
