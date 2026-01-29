using FastStart.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FastStart.Core;

/// <summary>
/// DI registration for core services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers core services.
    /// </summary>
    public static IServiceCollection AddFastStartCore(this IServiceCollection services)
    {
        services.AddSingleton<IAppTokenizer, AppTokenizer>();
        services.AddSingleton<FuzzyScorer>();
        services.AddSingleton<ISearchService, SearchService>();
        services.AddSingleton<ITelemetryConsentService, TelemetryConsentService>();
        services.AddHostedService<AppIndexService>();
        return services;
    }
}
