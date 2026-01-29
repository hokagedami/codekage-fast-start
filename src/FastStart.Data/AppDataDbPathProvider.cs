namespace FastStart.Data;

/// <summary>
/// Resolves database paths under %APPDATA%\\FastStart.
/// </summary>
public sealed class AppDataDbPathProvider : IDbPathProvider
{
    /// <inheritdoc />
    public string BaseDirectory { get; }

    /// <inheritdoc />
    public string DatabasePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppDataDbPathProvider"/> class.
    /// </summary>
    public AppDataDbPathProvider()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        BaseDirectory = Path.Combine(appData, "FastStart");
        DatabasePath = Path.Combine(BaseDirectory, "faststart.db");
    }
}
