using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;

namespace FastStart.UI.Services;

public sealed class ThemeService
{
    private Window? _window;
    public ElementTheme CurrentTheme { get; private set; }

    public void Initialize(Window window)
    {
        _window = window;
    }

    public void SetTheme(ElementTheme theme)
    {
        if (_window?.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme;
        }
        CurrentTheme = theme;
    }

    public void FollowSystem()
    {
        var uiSettings = new UISettings();
        var isDark = uiSettings.GetColorValue(UIColorType.Background).R < 128;
        SetTheme(isDark ? ElementTheme.Dark : ElementTheme.Light);
    }
}