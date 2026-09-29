using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sigei.Web.Components;
using Sigei.Web.Components.Account;
using Sigei.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
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
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ScopedTenantAccessor>();
builder.Services.AddScoped<ITenantAccessor>(sp => sp.GetRequiredService<ScopedTenantAccessor>());
builder.Services.AddAuthorization();
builder.Services.Configure<Microsoft.AspNetCore.Authorization.AuthorizationOptions>(Politicas.Registar);
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissaoHandler>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddClaimsPrincipalFactory<SigeiClaimsFactory>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    await DadosDemonstracao.CriarAsync(app.Services);
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


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

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
