using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sigei.Domain;

namespace Sigei.Web.Data;

/// <summary>
/// Garante que existe pelo menos um administrador da plataforma. Em produção as credenciais vêm da configuração
/// (variáveis de ambiente <c>Plataforma__Administrador__Utilizador</c> e <c>Plataforma__Administrador__Palavrapasse</c>),
/// nunca do código, e a palavra-passe tem de ser mudada no primeiro acesso.
/// </summary>
public static class AdministradorInicial
{
    public const string UtilizadorDemonstracao = "admin.sigei";
    public const string PalavrapasseDemonstracao = "Sigei@Admin2026";

    public static async Task GarantirAsync(IServiceProvider services, IConfiguration config, IHostEnvironment env)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("AdministradorInicial");

        await db.Database.MigrateAsync();
        if (await db.Users.AnyAsync(u => u.Perfil == PerfilUtilizador.AdminSaas)) return;

        var utilizador = config["Plataforma:Administrador:Utilizador"];
        var palavrapasse = config["Plataforma:Administrador:Palavrapasse"];
        var demonstracao = env.IsDevelopment() && string.IsNullOrWhiteSpace(palavrapasse);
        if (demonstracao)
        {
            utilizador = UtilizadorDemonstracao;
            palavrapasse = PalavrapasseDemonstracao;
        }
        if (string.IsNullOrWhiteSpace(utilizador) || string.IsNullOrWhiteSpace(palavrapasse))
        {
            logger.LogWarning("Não existe administrador da plataforma. Defina Plataforma__Administrador__Utilizador e __Palavrapasse.");
            return;
        }

        var nome = NomesDeUtilizador.Normalizar(utilizador);
        if (NomesDeUtilizador.Validar(nome) is { } erro)
            throw new InvalidOperationException("Utilizador do administrador inválido: " + erro);

        var admin = new ApplicationUser
        {
            UserName = nome, Perfil = PerfilUtilizador.AdminSaas, NomeCompleto = "Administrador da plataforma",
            MudarPalavrapasse = !demonstracao
        };
        var r = await users.CreateAsync(admin, palavrapasse);
        if (!r.Succeeded)
            throw new InvalidOperationException("Não foi possível criar o administrador: " + string.Join("; ", r.Errors.Select(e => e.Description)));
        logger.LogInformation("Administrador da plataforma '{Utilizador}' criado.", nome);
    }
}
