using System.Net;
using System.Text.Json;
using DeezerStats.Api.Middleware;
using DeezerStats.Application.Common.Exceptions;
using DeezerStats.Domain.SeedWork;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.UnitTests.Middleware
{
    public class ExceptionHandlingMiddlewareTests
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        [Fact]
        public async Task InvokeAsyncWhenDomainExceptionIsThrownShouldReturn400BadRequestWithProblemDetails()
        {
            // Arrange
            static async Task Next(HttpContext ctx) => throw new DomainException("L'ISRC fourni est invalide.");

            var middleware = new ExceptionHandlingMiddleware(Next);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            ProblemDetails? problemDetails = await ReadProblemDetailsAsync(context);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Violation de règle métier");
            problemDetails.Detail.Should().Be("L'ISRC fourni est invalide.");
        }

        [Fact]
        public async Task InvokeAsyncWhenConflictExceptionIsThrownShouldReturn409ConflictWithProblemDetails()
        {
            // Arrange : c'est notamment le cas de "email déjà utilisé" à l'inscription, qui doit
            // être distingué d'une simple erreur de validation (voir RegisterUserUseCase).
            static async Task Next(HttpContext ctx) => throw new ConflictException("Un utilisateur existe déjà avec cette adresse email.");

            var middleware = new ExceptionHandlingMiddleware(Next);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);

            ProblemDetails? problemDetails = await ReadProblemDetailsAsync(context);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Conflit");
            problemDetails.Detail.Should().Be("Un utilisateur existe déjà avec cette adresse email.");
        }

        [Fact]
        public async Task InvokeAsyncWhenValidationExceptionIsThrownShouldReturn400BadRequestWithFormattedErrors()
        {
            // Arrange
            var failures = new List<ValidationFailure>
            {
                new("Email", "L'adresse email n'est pas valide."),
            };

            // Fonction locale non statique (car elle capture 'failures')
            async Task Next(HttpContext ctx) => throw new ValidationException(failures);

            var middleware = new ExceptionHandlingMiddleware(Next);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

            ProblemDetails? problemDetails = await ReadProblemDetailsAsync(context);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Erreur de validation");
            problemDetails.Extensions.Should().ContainKey("errors");
        }

        private static async Task<ProblemDetails?> ReadProblemDetailsAsync(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseText = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<ProblemDetails>(responseText, _jsonOptions);
        }
    }
}
