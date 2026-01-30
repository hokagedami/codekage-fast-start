using FastStart.Core.Models;
using FastStart.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace FastStart.Core.Services;

/// <summary>
/// Service that indexes applications.
/// </summary>
public sealed class AppIndexService
{
    private readonly IShortcutParser _shortcutParser;
    private readonly IUwpAppEnumerator _uwpAppEnumerator;
    private readonly IRegistryAppEnumerator _registryAppEnumerator;
    private readonly IProgramFilesScanner _programFilesScanner;
    private readonly IAppTokenizer _tokenizer;
    private readonly IAppRepository _appRepository;
    private readonly ILogger<AppIndexService> _logger;
    private CancellationTokenSource? _cts;
    private Task? _indexingTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppIndexService"/> class.
    /// </summary>
    public AppIndexService(
        IShortcutParser shortcutParser,
        IUwpAppEnumerator uwpAppEnumerator,
        IRegistryAppEnumerator registryAppEnumerator,
        IProgramFilesScanner programFilesScanner,
        IAppTokenizer tokenizer,
        IAppRepository appRepository,
        ILogger<AppIndexService> logger)
    {
        _shortcutParser = shortcutParser;
        _uwpAppEnumerator = uwpAppEnumerator;
        _registryAppEnumerator = registryAppEnumerator;
        _programFilesScanner = programFilesScanner;
        _tokenizer = tokenizer;
        _appRepository = appRepository;
        _logger = logger;
    }

    /// <summary>
    /// Starts the indexing service.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _indexingTask = ExecuteAsync(_cts.Token);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the indexing service.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            if (_indexingTask is not null)
            {
                try
                {
                    await _indexingTask.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected
                }
            }
            _cts.Dispose();
        }
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var entries = new List<AppIndexEntry>(512);
            var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Scan shortcuts first (highest priority - user's preferred apps)
            await ScanShortcutLocationsAsync(entries, seenPaths, stoppingToken).ConfigureAwait(false);
            _logger.LogDebug("Found {Count} apps from shortcuts.", entries.Count);

            // Scan UWP apps
            var countBefore = entries.Count;
            await ScanUwpAppsAsync(entries, seenPaths, stoppingToken).ConfigureAwait(false);
            _logger.LogDebug("Found {Count} UWP apps.", entries.Count - countBefore);

            // Scan registry for installed apps
            countBefore = entries.Count;
            await ScanRegistryAppsAsync(entries, seenPaths, stoppingToken).ConfigureAwait(false);
            _logger.LogDebug("Found {Count} apps from registry.", entries.Count - countBefore);

            // Scan Program Files (lowest priority - catches anything missed)
            countBefore = entries.Count;
            await ScanProgramFilesAsync(entries, seenPaths, stoppingToken).ConfigureAwait(false);
            _logger.LogDebug("Found {Count} apps from Program Files.", entries.Count - countBefore);

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

    private async Task ScanShortcutLocationsAsync(List<AppIndexEntry> entries, HashSet<string> seenPaths, CancellationToken ct)
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

                    // Skip if we've already seen this executable
                    if (!seenPaths.Add(app.ExecutablePath))
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

    private async Task ScanUwpAppsAsync(List<AppIndexEntry> entries, HashSet<string> seenPaths, CancellationToken ct)
    {
        await foreach (var app in _uwpAppEnumerator.EnumerateAsync(ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();

            // UWP apps use AUMID as ExecutablePath, so we track by name for dedup
            if (!seenPaths.Add($"uwp:{app.Name}"))
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

    private async Task ScanRegistryAppsAsync(List<AppIndexEntry> entries, HashSet<string> seenPaths, CancellationToken ct)
    {
        try
        {
            await foreach (var app in _registryAppEnumerator.EnumerateAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();

                // Skip if we've already seen this executable
                if (!seenPaths.Add(app.ExecutablePath))
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
            _logger.LogWarning(ex, "Failed to scan registry for applications.");
        }
    }

    private async Task ScanProgramFilesAsync(List<AppIndexEntry> entries, HashSet<string> seenPaths, CancellationToken ct)
    {
        try
        {
            await foreach (var app in _programFilesScanner.ScanAsync(ct).ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();

                // Skip if we've already seen this executable
                if (!seenPaths.Add(app.ExecutablePath))
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
            _logger.LogWarning(ex, "Failed to scan Program Files for applications.");
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
