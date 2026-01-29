using FastStart.Core.Models;
using FastStart.Core.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FastStart.Core.Services;

/// <summary>
/// Background service that indexes applications.
/// </summary>
public sealed class AppIndexService : BackgroundService
{
    private readonly IShortcutParser _shortcutParser;
    private readonly IUwpAppEnumerator _uwpAppEnumerator;
    private readonly IAppTokenizer _tokenizer;
    private readonly IAppRepository _appRepository;
    private readonly ILogger<AppIndexService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppIndexService"/> class.
    /// </summary>
    public AppIndexService(
        IShortcutParser shortcutParser,
        IUwpAppEnumerator uwpAppEnumerator,
        IAppTokenizer tokenizer,
        IAppRepository appRepository,
        ILogger<AppIndexService> logger)
    {
        _shortcutParser = shortcutParser;
        _uwpAppEnumerator = uwpAppEnumerator;
        _tokenizer = tokenizer;
        _appRepository = appRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var entries = new List<AppIndexEntry>(512);
            await ScanShortcutLocationsAsync(entries, stoppingToken).ConfigureAwait(false);
            await ScanUwpAppsAsync(entries, stoppingToken).ConfigureAwait(false);

            if (entries.Count == 0)
            {
                _logger.LogInformation("No applications discovered during indexing.");
                return;
            }

            await _appRepository.UpsertAppsWithTokensAsync(entries, stoppingToken).ConfigureAwait(false);
            _logger.LogInformation("Indexed {Count} applications.", entries.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Indexing cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Indexing failed.");
        }
    }

    private async Task ScanShortcutLocationsAsync(List<AppIndexEntry> entries, CancellationToken ct)
    {
        foreach (var directory in GetShortcutDirectories())
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            if (!Directory.Exists(directory))
            {
                continue;
            }

            try
            {
                var shortcutPaths = await Task.Run(
                    () => Directory.EnumerateFiles(directory, "*.lnk", SearchOption.AllDirectories).ToArray(),
                    ct).ConfigureAwait(false);

                foreach (var path in shortcutPaths)
                {
                    ct.ThrowIfCancellationRequested();

                    var app = await _shortcutParser.ParseAsync(path, ct).ConfigureAwait(false);
                    if (app is null)
                    {
                        continue;
                    }

                    app.LastIndexedUtc = DateTimeOffset.UtcNow;
                    entries.Add(new AppIndexEntry
                    {
                        App = app,
                        Tokens = BuildTokens(app)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan shortcuts in {Directory}.", directory);
            }
        }
    }

    private async Task ScanUwpAppsAsync(List<AppIndexEntry> entries, CancellationToken ct)
    {
        await foreach (var app in _uwpAppEnumerator.EnumerateAsync(ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();

            app.LastIndexedUtc = DateTimeOffset.UtcNow;
            entries.Add(new AppIndexEntry
            {
                App = app,
                Tokens = BuildTokens(app)
            });
        }
    }

    private IReadOnlyList<string> BuildTokens(AppInfo app)
    {
        var tokenSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in _tokenizer.Tokenize(app.Name))
        {
            tokenSet.Add(token);
        }

        if (!string.IsNullOrWhiteSpace(app.ExecutablePath))
        {
            var fileName = Path.GetFileNameWithoutExtension(app.ExecutablePath);
            foreach (var token in _tokenizer.Tokenize(fileName))
            {
                tokenSet.Add(token);
            }
        }

        return tokenSet.Count == 0 ? Array.Empty<string>() : tokenSet.ToArray();
    }

    private static IEnumerable<string> GetShortcutDirectories()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        yield return Path.Combine(appData, "Microsoft", "Windows", "Start Menu", "Programs");
        yield return Path.Combine(commonAppData, "Microsoft", "Windows", "Start Menu", "Programs");
        yield return Path.Combine(userProfile, "Desktop");
        yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
    }
}
