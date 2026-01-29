using FastStart.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FastStart.Tests;

public sealed class DatabaseInitializerTests
{
    [Fact]
    public async Task Initialize_RebuildsOnCorruption()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "FastStartTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(baseDir);

        var dbPath = Path.Combine(baseDir, "faststart.db");
        var pathProvider = new TestDbPathProvider(baseDir, dbPath);

        var services = new ServiceCollection();
        services.AddDbContextFactory<FastStartDbContext>(options =>
        {
            // Use Pooling=False to prevent connection caching during tests
            options.UseSqlite($"Data Source={dbPath};Pooling=False");
        });

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<FastStartDbContext>>();
        var initializer = new DatabaseInitializer(factory, pathProvider, NullLogger<DatabaseInitializer>.Instance);

        // First initialization - creates the database
        await initializer.InitializeAsync(CancellationToken.None);
        Assert.True(File.Exists(dbPath));

        // Clear any pooled connections before corrupting the file
        SqliteConnection.ClearAllPools();

        // Corrupt the database by overwriting with garbage
        await File.WriteAllBytesAsync(dbPath, new byte[] { 0x00, 0x01, 0x02 });

        // Second initialization - should detect corruption and rebuild
        await initializer.InitializeAsync(CancellationToken.None);

        // Verify the database was rebuilt with valid schema
        await using var db = await factory.CreateDbContextAsync();
        var hasSchema = await db.SchemaVersions.AnyAsync();
        Assert.True(hasSchema);
    }

    private sealed class TestDbPathProvider : IDbPathProvider
    {
        public TestDbPathProvider(string baseDirectory, string databasePath)
        {
            BaseDirectory = baseDirectory;
            DatabasePath = databasePath;
        }

        public string BaseDirectory { get; }

        public string DatabasePath { get; }
    }
}
