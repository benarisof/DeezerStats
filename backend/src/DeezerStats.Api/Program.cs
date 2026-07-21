using System.Text;
using DeezerStats.Api.Middleware;
using DeezerStats.Application;
using DeezerStats.Infrastructure;
using DeezerStats.Infrastructure.Adapters.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
// (ex. http://localhost:5173 vs http://localhost:5231), donc sans policy explicite le navigateur
// bloque tous les appels. Les origines autorisées sont configurables via "Cors:AllowedOrigins"
// (voir appsettings.*.json et docker-compose.yml) plutôt que codées en dur.
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

// JWT : AddAuthentication/AddJwtBearer/FallbackPolicy sont désormais toujours configurés (plus de
// "if (jwtSettings != null)"). Ce garde-fou pouvait laisser l'API démarrer sans AUCUNE
// authentification si la section "Jwt" venait à manquer (déploiement incomplet, variable
// d'environnement mal nommée — on a déjà eu ce problème avec Jwt__Secret vs Jwt__Key), sans la
// moindre erreur au démarrage. La validation de la config (voir JwtSettingsValidator, enregistrée
// dans AddInfrastructure via ValidateOnStart()) fait maintenant échouer le démarrage avec un
// message explicite si elle est absente ou invalide.
builder.Services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

// Configuration différée via IOptions<JwtSettings> : construite seulement au moment où
// JwtBearerOptions est effectivement résolu, c'est-à-dire après que ValidateOnStart() a validé la
// config au démarrage. Évite de tenter de construire une SymmetricSecurityKey à partir d'une clé
// vide avant même que la validation n'ait eu la chance d'échouer proprement.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((bearerOptions, jwtSettingsOptions) =>
    {
        JwtSettings jwtSettings = jwtSettingsOptions.Value;

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

// FallbackPolicy : protège TOUTES les routes par défaut (utilisateur authentifié requis), sauf
// celles explicitement marquées [AllowAnonymous] (ex. AuthController). Sans ça, un futur
// controller (stats, tops, historique...) resterait accessible anonymement s'il oublie
// d'ajouter [Authorize] — le comportement par défaut d'ASP.NET Core est anonyme.
builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

WebApplication app = builder.Build();

// Garde-fou complémentaire à JwtSettingsValidator : celui-ci valide la forme de Jwt:Key (non vide,
// longueur suffisante), mais ne peut pas savoir qu'une clé par ailleurs valide est simplement le
// placeholder de développement d'appsettings.json. On refuse explicitement de démarrer avec cette
// valeur en dehors de Development, pour ne jamais signer de tokens avec un secret public.
if (!app.Environment.IsDevelopment())
{
    JwtSettings jwtSettings = app.Services.GetRequiredService<IOptions<JwtSettings>>().Value;

    if (jwtSettings.Key == JwtSettings.DevelopmentPlaceholderKey)
    {
        throw new InvalidOperationException(
            "Jwt:Key utilise encore la valeur de développement par défaut (voir appsettings.json). " +
            "Positionnez la variable d'environnement JWT_SECRET (voir docker-compose.yml) avant de " +
            "démarrer en dehors de Development.");
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
