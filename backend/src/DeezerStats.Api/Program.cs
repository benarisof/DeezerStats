using System.Text;
using DeezerStats.Api.Middleware;
using DeezerStats.Application;
using DeezerStats.Infrastructure;
using DeezerStats.Infrastructure.Adapters.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

JwtSettings? jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();

if (jwtSettings != null)
{
    builder.Services.AddAuthentication(defaultScheme: JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
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
}

WebApplication app = builder.Build();

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
