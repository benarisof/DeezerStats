using System.Net;
using System.Text.Json;
using DeezerStats.Application.Common.Exceptions;
using DeezerStats.Domain.SeedWork;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private static readonly Action<ILogger, string?, string?, Exception?> _logUnexpectedError =
            LoggerMessage.Define<string?, string?>(
                logLevel: LogLevel.Error,
                eventId: new EventId(1001, "UnexpectedError"),
                formatString: "Erreur non gérée lors du traitement de la requête {Method} {Path}.");

        private readonly RequestDelegate _next = next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json";

            ProblemDetails problemDetails = exception switch
            {
                ValidationException validationEx => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Erreur de validation",
                    Detail = "Une ou plusieurs règles de validation n'ont pas été respectées.",
                    Extensions =
                    {
                        ["errors"] = validationEx.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(
                                g => g.Key,
                                g => g.Select(e => e.ErrorMessage).ToArray()),
                    },
                },

                ConflictException conflictEx => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Conflict,
                    Title = "Conflit",
                    Detail = conflictEx.Message,
                },

                AuthenticationFailedException authFailedEx => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.Unauthorized,
                    Title = "Authentification refusée",
                    Detail = authFailedEx.Message,
                },

                DomainException domainEx => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Violation de règle métier",
                    Detail = domainEx.Message,
                },

                _ => HandleUnexpectedException(exception),
            };

            context.Response.StatusCode = problemDetails.Status!.Value;
            var json = JsonSerializer.Serialize(problemDetails, _jsonOptions);
            return context.Response.WriteAsync(json);
        }

        private ProblemDetails HandleUnexpectedException(Exception exception)
        {
            _logUnexpectedError(_logger, exception.Source, exception.TargetSite?.ToString(), exception);

            return new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Erreur interne du serveur",
                Detail = "Une erreur inattendue est survenue. Veuillez réessayer ultérieurement.",
            };
        }
    }
}
