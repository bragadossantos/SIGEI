using Microsoft.AspNetCore.Identity;
using Sigei.Domain;

namespace Sigei.Web.Data;

public class ApplicationUser : IdentityUser
{
    /// <summary>Escola a que o utilizador pertence. Nulo apenas para o administrador do SaaS.</summary>
    public int? EscolaId { get; set; }
    public PerfilUtilizador Perfil { get; set; } = PerfilUtilizador.Encarregado;
    public string? NomeCompleto { get; set; }

    /// <summary>Verdadeiro depois de a conta ser criada ou de a palavra-passe ser reposta por outra pessoa.</summary>
    public bool MudarPalavrapasse { get; set; }
}
