using FastStart.Core.Repositories;
using FastStart.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FastStart.Data;

/// <summary>
/// DI registration for data services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers data services.
    /// </summary>
    public static IServiceCollection AddFastStartData(this IServiceCollection services)
    {
        services.AddSingleton<IDbPathProvider, AppDataDbPathProvider>();
        services.AddDbContextFactory<FastStartDbContext>((sp, options) =>
        {
            var pathProvider = sp.GetRequiredService<IDbPathProvider>();
            options.UseSqlite($"Data Source={pathProvider.DatabasePath};Cache=Shared");
        });

        // Register AppRepository as inner, wrapped with caching decorator
        services.AddSingleton<AppRepository>();
        services.AddSingleton<IAppRepository>(sp =>
        {
            var inner = sp.GetRequiredService<AppRepository>();
            var logger = sp.GetRequiredService<ILogger<CachingAppRepository>>();
            return new CachingAppRepository(inner, logger);
        });
        services.AddSingleton<IPinRepository, PinRepository>();
        services.AddSingleton<IPreferencesRepository, PreferencesRepository>();
        services.AddSingleton<IRecentLaunchRepository, RecentLaunchRepository>();
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddHostedService<DatabaseInitializerHostedService>();

        return services;
    }
}
