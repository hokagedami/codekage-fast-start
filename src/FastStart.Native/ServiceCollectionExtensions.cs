using FastStart.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FastStart.Native;

/// <summary>
/// DI registration for native services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers native services.
    /// </summary>
    public static IServiceCollection AddFastStartNative(this IServiceCollection services)
    {
        services.AddSingleton<IShortcutParser, ShellLinkShortcutParser>();
        services.AddSingleton<IUwpAppEnumerator, PackageManagerUwpAppEnumerator>();
        services.AddSingleton<IAppLauncher, AppLauncher>();
        return services;
    }
}
