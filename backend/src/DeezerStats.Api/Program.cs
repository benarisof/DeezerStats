using System.Text;
using DeezerStats.Api.Middleware;
using DeezerStats.Application;
using DeezerStats.Infrastructure;
using DeezerStats.Infrastructure.Adapters.Search;
using DeezerStats.Infrastructure.Adapters.Security;
using DeezerStats.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

const string FrontendCorsPolicy = "Frontend";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 1. Contrôleurs & OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 2. Injection de la couche Application
builder.Services.AddApplication();

// 3. Injection de la couche Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// 4. CORS : le frontend (SPA React/Vite) est servi sur une origine différente de l'API
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((bearerOptions, jwtSettingsOptions) =>
    {
        JwtSettings jwtSettings = jwtSettingsOptions.Value;

        bearerOptions.MapInboundClaims = false;

        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key)),
        };
    });

builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

WebApplication app = builder.Build();

// Applique automatiquement les migrations EF Core en attente au démarrage, pour qu'un utilisateur
// n'ait jamais à lancer "dotnet ef database update" lui-même (voir docker-compose.yml : la base
// Postgres démarre vide au premier "docker compose up"). Idempotent : sans effet si tout est déjà
// à jour, donc sans risque à chaque redémarrage de "dotnet watch". Ignoré par le provider EF Core
// InMemory (voir CustomWebApplicationFactory dans les tests d'intégration), qui ne supporte pas les
// migrations relationnelles.
using (IServiceScope migrationScope = app.Services.CreateScope())
{
    ApplicationDbContext dbContext = migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }
}

// Aucun secret de appsettings.json (valeurs de développement volontairement publiques dans le
// dépôt) ne doit se retrouver actif en dehors de Development : voir JwtSettings.DevelopmentPlaceholderKey
// et MeilisearchOptions.DevelopmentPlaceholderMasterKey. Un déploiement en production qui omettrait
// de positionner la variable d'environnement correspondante doit échouer bruyamment au démarrage
// plutôt que de tourner silencieusement avec un secret trivial et public.
if (!app.Environment.IsDevelopment())
{
    JwtSettings jwtSettings = app.Services.GetRequiredService<IOptions<JwtSettings>>().Value;
    EnsureNotDevelopmentPlaceholder("Jwt:Key", jwtSettings.Key, JwtSettings.DevelopmentPlaceholderKey, "JWT_SECRET");

    MeilisearchOptions meilisearchOptions = app.Services.GetRequiredService<IOptions<MeilisearchOptions>>().Value;
    EnsureNotDevelopmentPlaceholder("Meilisearch:MasterKey", meilisearchOptions.MasterKey, MeilisearchOptions.DevelopmentPlaceholderMasterKey, "MEILI_MASTER_KEY");
}

static void EnsureNotDevelopmentPlaceholder(string settingName, string actualValue, string placeholderValue, string environmentVariableName)
{
    if (actualValue == placeholderValue)
    {
        throw new InvalidOperationException(
            $"{settingName} utilise encore la valeur de développement par défaut (voir appsettings.json). " +
            $"Positionnez la variable d'environnement {environmentVariableName} (voir docker-compose.yml) " +
            "avant de démarrer en dehors de Development.");
    }
}

// Configure le pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
