using DeezerStats.Application.UseCases.Imports;
using DeezerStats.Application.UseCases.Tracks;
using DeezerStats.Application.UseCases.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DeezerStats.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

            // Use cases. NB : IRegisterUserUseCase/IAuthenticateUserUseCase n'étaient jusqu'ici
            // enregistrés nulle part dans le conteneur DI, alors qu'AuthController en dépend : les
            // routes /auth/register et /auth/login auraient échoué au premier appel réel avec une
            // InvalidOperationException ("Unable to resolve service"), un problème que les tests
            // unitaires (qui instancient les use cases directement) ne pouvaient pas révéler.
            services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
            services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();
            services.AddScoped<ImportListeningHistoryUseCase>();
            services.AddScoped<GetOrEnrichTrackUseCase>();

            return services;
        }
    }
}
