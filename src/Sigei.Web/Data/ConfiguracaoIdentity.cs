using Microsoft.AspNetCore.Identity;

namespace Sigei.Web.Data;

public static class ConfiguracaoIdentity
{
    /// <summary>Regras de contas partilhadas pela aplicação e pelos testes.</summary>
    public static void Aplicar(IdentityOptions o)
    {
        o.SignIn.RequireConfirmedAccount = false; // as contas são criadas pela escola, não há registo público
        o.User.RequireUniqueEmail = false;        // o acesso é por telefone ou nome de utilizador
        o.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyz0123456789._-";

        o.Password.RequiredLength = 8;
        o.Password.RequireDigit = true;
        o.Password.RequireLowercase = true;
        o.Password.RequireUppercase = true;
        o.Password.RequireNonAlphanumeric = true;

        o.Lockout.AllowedForNewUsers = true;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    }
}
