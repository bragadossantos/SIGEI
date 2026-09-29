using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sigei.Web.Components;
using Sigei.Web.Components.Account;
using Sigei.Web.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.Configure<BackupOptions>(builder.Configuration.GetSection("Backups"));
builder.Services.AddScoped<BackupAnoLetivoService>();
builder.Services.AddScoped<GestaoContas>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ScopedTenantAccessor>();
builder.Services.AddScoped<ITenantAccessor>(sp => sp.GetRequiredService<ScopedTenantAccessor>());
builder.Services.AddAuthorization();
builder.Services.Configure<Microsoft.AspNetCore.Authorization.AuthorizationOptions>(Politicas.Registar);
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissaoHandler>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(ConfiguracaoIdentity.Aplicar)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddClaimsPrincipalFactory<SigeiClaimsFactory>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    await DadosDemonstracao.CriarAsync(app.Services);
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // O valor predefinido do HSTS é 30 dias.
    app.UseHsts();
}

await AdministradorInicial.GarantirAsync(app.Services, app.Configuration, app.Environment);

app.UseHttpsRedirection();

app.UseAuthentication();

// Quem tem uma palavra-passe temporária só pode mudá-la (ou terminar sessão) antes de usar o sistema.
app.Use(async (context, next) =>
{
    var caminho = context.Request.Path;
    var isento = caminho.StartsWithSegments("/Account") || caminho.StartsWithSegments("/_blazor")
                 || caminho.StartsWithSegments("/_framework") || caminho.StartsWithSegments("/_content")
                 || Path.HasExtension(caminho.Value);
    if (!isento && HttpMethods.IsGet(context.Request.Method)
        && context.User.HasClaim(c => c.Type == SigeiClaims.MudarPalavrapasse))
    {
        context.Response.Redirect("/Account/Manage/ChangePassword");
        return;
    }
    await next();
});

app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Descarga de backups: só a direção, e só ficheiros da própria escola.
app.MapGet("/direcao/backups/{ficheiro}", (string ficheiro, System.Security.Claims.ClaimsPrincipal user,
        Microsoft.Extensions.Options.IOptions<BackupOptions> opcoes) =>
    {
        if (!int.TryParse(user.FindFirst(SigeiClaims.EscolaId)?.Value, out var escolaId)
            || !BackupAnoLetivoService.PertenceAEscola(ficheiro, escolaId))
            return Results.NotFound();
        var caminho = Path.Combine(Path.GetFullPath(opcoes.Value.Pasta), ficheiro);
        return File.Exists(caminho) ? Results.File(caminho, "application/json", ficheiro) : Results.NotFound();
    })
    .RequireAuthorization(Politicas.AbrirFecharAnoLetivo);

app.MapAdditionalIdentityEndpoints();

app.Run();
