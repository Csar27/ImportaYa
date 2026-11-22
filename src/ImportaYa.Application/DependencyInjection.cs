using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Services;
using ImportaYa.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace ImportaYa.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ImportService>();
        services.AddScoped<IImportProcessor, ImportProcessor>();
        services.AddScoped<ImportQueryService>();
        services.AddScoped<UserQueryService>();
        services.AddScoped<IUserRowValidator, UserRowValidator>();
        services.AddScoped<AuthService>();
        services.AddScoped<AccountService>();
        services.AddScoped<AccountSeeder>();
        return services;
    }
}
