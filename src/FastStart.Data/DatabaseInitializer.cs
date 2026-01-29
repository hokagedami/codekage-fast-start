using FastStart.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FastStart.Data;

/// <summary>
/// Handles database initialization and corruption recovery.
/// </summary>
public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IDbContextFactory<FastStartDbContext> _dbContextFactory;
    private readonly IDbPathProvider _pathProvider;
    private readonly ILogger<DatabaseInitializer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseInitializer"/> class.
    /// </summary>
    public DatabaseInitializer(
        IDbContextFactory<FastStartDbContext> dbContextFactory,
        IDbPathProvider pathProvider,
        ILogger<DatabaseInitializer> logger)
    {
        _dbContextFactory = dbContextFactory;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(_pathProvider.BaseDirectory);

        try
        {
            await MigrateAsync(ct).ConfigureAwait(false);
        }
        catch (SqliteException ex) when (IsCorruption(ex))
        {
            _logger.LogWarning(ex, "Database corruption detected. Rebuilding.");
            await RebuildAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task MigrateAsync(CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await db.Database.MigrateAsync(ct).ConfigureAwait(false);

        var schema = await db.SchemaVersions.FirstOrDefaultAsync(version => version.Id == 1, ct).ConfigureAwait(false);
        if (schema is null)
        {
            db.SchemaVersions.Add(new SchemaVersionEntity
            {
                Id = 1,
                Version = 1,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task RebuildAsync(CancellationToken ct)
    {
        var path = _pathProvider.DatabasePath;

        // Clear all pooled connections to release file locks and reset EF Core's cached state
        SqliteConnection.ClearAllPools();

        if (File.Exists(path))
        {
            var backupPath = path + ".corrupt-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            File.Move(path, backupPath, overwrite: true);
        }

        // Use EnsureCreatedAsync for rebuild since the database is brand new
        // This creates the schema based on the current model without migration history
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await db.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);

        // Add schema version record
        db.SchemaVersions.Add(new SchemaVersionEntity
        {
            Id = 1,
            Version = 1,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static bool IsCorruption(SqliteException ex)
    {
        // SQLite error codes:
        // 1  = SQLITE_ERROR (generic error, includes "no such table" after failed migration)
        // 11 = SQLITE_CORRUPT (database disk image is malformed)
        // 26 = SQLITE_NOTADB (file is not a database)
        return ex.SqliteErrorCode == 11
               || ex.SqliteErrorCode == 26
               || ex.Message.Contains("malformed", StringComparison.OrdinalIgnoreCase)
               || ex.Message.Contains("corrupt", StringComparison.OrdinalIgnoreCase)
               || ex.Message.Contains("no such table", StringComparison.OrdinalIgnoreCase)
               || ex.Message.Contains("file is not a database", StringComparison.OrdinalIgnoreCase);
    }
}
