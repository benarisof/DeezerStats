using DeezerStats.Api.Middleware;
using DeezerStats.Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 1. Contrôleurs & OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 2. Injection de toute la couche Infrastructure (EF Core, Repositories, Adapters)
builder.Services.AddInfrastructure(builder.Configuration);

WebApplication app = builder.Build();

// Configure le pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
