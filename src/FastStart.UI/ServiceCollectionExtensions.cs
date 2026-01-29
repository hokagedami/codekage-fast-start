using FastStart.UI.Services;
using FastStart.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FastStart.UI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFastStartUI(this IServiceCollection services)
    {
        services.AddSingleton<IconService>();
        services.AddSingleton<ThemeService>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();
        return services;
    }
}
