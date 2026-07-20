using System.Net;
using System.Text.Json;
using DeezerStats.Application.Common.Exceptions;
using DeezerStats.Domain.SeedWork;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DeezerStats.Api.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next)
    {
        // Instance unique, réutilisée pour toutes les sérialisations
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly RequestDelegate _next = next;

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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
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

                DomainException domainEx => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Violation de règle métier",
                    Detail = domainEx.Message,
                },

                NotFoundException notFoundEx => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.NotFound,
                    Title = "Ressource non trouvée",
                    Detail = notFoundEx.Message,
                },

                _ => new ProblemDetails
                {
                    Status = (int)HttpStatusCode.InternalServerError,
                    Title = "Erreur interne du serveur",
                    Detail = "Une erreur inattendue est survenue. Veuillez réessayer ultérieurement.",
                },
            };

            context.Response.StatusCode = problemDetails.Status!.Value;

            // Utilisation de l'instance statique
            var json = JsonSerializer.Serialize(problemDetails, _jsonOptions);

            return context.Response.WriteAsync(json);
        }
    }
}
