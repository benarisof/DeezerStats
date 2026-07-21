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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

            var middleware = new ExceptionHandlingMiddleware(Next, NullLogger<ExceptionHandlingMiddleware>.Instance);
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

            var middleware = new ExceptionHandlingMiddleware(Next, NullLogger<ExceptionHandlingMiddleware>.Instance);
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

            var middleware = new ExceptionHandlingMiddleware(Next, NullLogger<ExceptionHandlingMiddleware>.Instance);
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

        [Fact]
        public async Task InvokeAsyncWhenAuthenticationFailedExceptionIsThrownShouldReturn401UnauthorizedWithProblemDetails()
        {
            // Arrange
            static async Task Next(HttpContext ctx) => throw new AuthenticationFailedException("Email ou mot de passe invalide.");

            var middleware = new ExceptionHandlingMiddleware(Next, NullLogger<ExceptionHandlingMiddleware>.Instance);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.Unauthorized);

            ProblemDetails? problemDetails = await ReadProblemDetailsAsync(context);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Authentification refusée");
            problemDetails.Detail.Should().Be("Email ou mot de passe invalide.");
        }

        [Fact]
        public async Task InvokeAsyncWhenUnexpectedExceptionIsThrownShouldReturn500AndLogTheException()
        {
            // Arrange : une exception qui n'appartient à aucune des hiérarchies connues du
            // middleware (ni DomainException, ni ValidationException) simule un bug/incident
            // réel — le seul cas qui doit être loggé en Error (voir ExceptionHandlingMiddleware).
            var thrownException = new InvalidOperationException("Erreur inattendue non gérée.");
            async Task Next(HttpContext ctx) => throw thrownException;

            var recordingLogger = new RecordingLogger();
            var middleware = new ExceptionHandlingMiddleware(Next, recordingLogger);
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            ProblemDetails? problemDetails = await ReadProblemDetailsAsync(context);
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Erreur interne du serveur");

            recordingLogger.LastLogLevel.Should().Be(LogLevel.Error);
            recordingLogger.LastException.Should().BeSameAs(thrownException);
        }

        private static async Task<ProblemDetails?> ReadProblemDetailsAsync(HttpContext context)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(context.Response.Body);
            var responseText = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<ProblemDetails>(responseText, _jsonOptions);
        }

        // Double de test pour ILogger<T> : NSubstitute peine à mocker Log<TState> à cause du
        // type interne FormattedLogValues utilisé par les méthodes d'extension (LogError, etc.),
        // donc une implémentation manuelle minimale est plus simple et plus fiable ici.
        private sealed class RecordingLogger : ILogger<ExceptionHandlingMiddleware>
        {
            public LogLevel? LastLogLevel { get; private set; }

            public Exception? LastException { get; private set; }

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                LastLogLevel = logLevel;
                LastException = exception;
            }
        }
    }
}
