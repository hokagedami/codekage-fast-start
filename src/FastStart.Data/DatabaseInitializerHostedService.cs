using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FastStart.Data;

/// <summary>
/// Hosted service that initializes the database.
/// </summary>
public sealed class DatabaseInitializerHostedService : IHostedService
{
    private readonly IDatabaseInitializer _initializer;
    private readonly ILogger<DatabaseInitializerHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseInitializerHostedService"/> class.
    /// </summary>
    public DatabaseInitializerHostedService(IDatabaseInitializer initializer, ILogger<DatabaseInitializerHostedService> logger)
    {
        _initializer = initializer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed.");
            throw;
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
