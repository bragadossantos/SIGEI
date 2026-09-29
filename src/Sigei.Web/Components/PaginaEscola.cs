using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Sigei.Web.Data;

namespace Sigei.Web.Components;

/// <summary>
/// Base das páginas que mostram dados de uma escola. Antes de carregar qualquer dado, identifica a escola
/// do utilizador — nas páginas interativas não há HttpContext, e sem isto o filtro devolveria zero registos.
/// </summary>
public abstract class PaginaEscola : ComponentBase
{
    [Inject] private AuthenticationStateProvider Autenticacao { get; set; } = default!;
    [Inject] private ScopedTenantAccessor Tenant { get; set; } = default!;

    protected string UserId { get; private set; } = "";
    protected int? EscolaId { get; private set; }

    protected sealed override async Task OnInitializedAsync()
    {
        var user = (await Autenticacao.GetAuthenticationStateAsync()).User;
        UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        EscolaId = int.TryParse(user.FindFirstValue(SigeiClaims.EscolaId), out var id) ? id : null;
        Tenant.Definir(EscolaId);
        await CarregarAsync();
    }

    protected abstract Task CarregarAsync();
}
