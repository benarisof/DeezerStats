using System.Text;
using DeezerStats.Api.Middleware;
using DeezerStats.Application;
using DeezerStats.Infrastructure;
using DeezerStats.Infrastructure.Adapters.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 1. Contrôleurs & OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 2. Injection de la couche Application
builder.Services.AddApplication();

// 3. Injection de la couche Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

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
    builder.Services.AddAuthorization();
}

WebApplication app = builder.Build();

// Configure le pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
