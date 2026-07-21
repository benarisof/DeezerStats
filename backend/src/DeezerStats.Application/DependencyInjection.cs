using DeezerStats.Application.UseCases.Imports;
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
            services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
            services.AddScoped<IAuthenticateUserUseCase, AuthenticateUserUseCase>();
            services.AddScoped<IImportListeningHistoryUseCase, ImportListeningHistoryUseCase>();

            return services;
        }
    }
}
