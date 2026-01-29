using FastStart.Core.Models;
using FastStart.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FastStart.Data;

/// <summary>
/// Entity Framework Core context for FastStart.
/// </summary>
public sealed class FastStartDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FastStartDbContext"/> class.
    /// </summary>
    public FastStartDbContext(DbContextOptions<FastStartDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets the applications table.
    /// </summary>
    public DbSet<AppEntity> Applications => Set<AppEntity>();

    /// <summary>
    /// Gets the application tokens table.
    /// </summary>
    public DbSet<AppTokenEntity> AppTokens => Set<AppTokenEntity>();

    /// <summary>
    /// Gets the pins table.
    /// </summary>
    public DbSet<PinEntity> UserPins => Set<PinEntity>();

    /// <summary>
    /// Gets the preferences table.
    /// </summary>
    public DbSet<PreferenceEntity> Preferences => Set<PreferenceEntity>();

    /// <summary>
    /// Gets the recent launches table.
    /// </summary>
    public DbSet<RecentLaunchEntity> RecentLaunches => Set<RecentLaunchEntity>();

    /// <summary>
    /// Gets the schema version table.
    /// </summary>
    public DbSet<SchemaVersionEntity> SchemaVersions => Set<SchemaVersionEntity>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppEntity>(entity =>
        {
            entity.ToTable("Applications");
            entity.HasKey(app => app.Id);
            entity.Property(app => app.Name).IsRequired();
            entity.Property(app => app.ExecutablePath).IsRequired();
            entity.Property(app => app.Source).HasConversion<string>();
            entity.HasIndex(app => app.Name).HasDatabaseName("idx_apps_name");
            entity.HasIndex(app => new { app.ExecutablePath, app.Arguments }).IsUnique();
        });

        modelBuilder.Entity<AppTokenEntity>(entity =>
        {
            entity.ToTable("AppTokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.Token).IsRequired();
            entity.HasIndex(token => token.Token).HasDatabaseName("idx_tokens_token");
            entity.HasOne(token => token.App)
                .WithMany(app => app.Tokens)
                .HasForeignKey(token => token.AppId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PinEntity>(entity =>
        {
            entity.ToTable("UserPins");
            entity.HasKey(pin => pin.Id);
            entity.HasIndex(pin => pin.Position).HasDatabaseName("idx_pins_position");
        });

        modelBuilder.Entity<PreferenceEntity>(entity =>
        {
            entity.ToTable("Preferences");
            entity.HasKey(pref => pref.Key);
        });

        modelBuilder.Entity<RecentLaunchEntity>(entity =>
        {
            entity.ToTable("RecentLaunches");
            entity.HasKey(launch => launch.Id);
            entity.HasIndex(launch => launch.ApplicationId).HasDatabaseName("idx_recent_app");
        });

        modelBuilder.Entity<SchemaVersionEntity>(entity =>
        {
            entity.ToTable("SchemaVersion");
            entity.HasKey(version => version.Id);
        });
    }
}
