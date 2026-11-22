using ImportaYa.Application.Abstractions;
using ImportaYa.Application.Options;
using ImportaYa.Infrastructure.Security;
using ImportaYa.Domain.Repositories;
using ImportaYa.Infrastructure.Background;
using ImportaYa.Infrastructure.Files;
using ImportaYa.Infrastructure.Persistence;
using ImportaYa.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImportaYa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ImportOptions>(configuration.GetSection(ImportOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AdminSeedOptions>(configuration.GetSection(AdminSeedOptions.SectionName));

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IImportJobRepository, ImportJobRepository>();
        services.AddScoped<IImportLogRepository, ImportLogRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IUserBulkWriter, UserBulkWriter>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        services.AddScoped<ILogExporter, LogExporter>();
        services.AddScoped<ITemplateGenerator, TemplateGenerator>();

        services.AddSingleton<IFileParser, CsvFileParser>();
        services.AddSingleton<IFileParser, ExcelFileParser>();
        services.AddSingleton<IFileParserFactory, FileParserFactory>();

        services.AddSingleton<ChannelImportQueue>();
        services.AddSingleton<IImportQueue>(sp => sp.GetRequiredService<ChannelImportQueue>());
        services.AddSingleton<IImportProgressReporter, NoOpImportProgressReporter>();
        services.AddHostedService<ImportWorker>();

        return services;
    }
}
