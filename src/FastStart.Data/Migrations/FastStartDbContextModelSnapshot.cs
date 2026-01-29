using System;
using FastStart.Core.Models;
using FastStart.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FastStart.Data.Migrations;

/// <inheritdoc />
public partial class FastStartDbContextModelSnapshot : ModelSnapshot
{
    /// <inheritdoc />
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.1");

        modelBuilder.Entity<AppEntity>(entity =>
        {
            entity.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            entity.Property<string>("Arguments")
                .HasColumnType("TEXT");

            entity.Property<string>("ExecutablePath")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property<string>("IconPath")
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset>("LastIndexedUtc")
                .HasColumnType("TEXT");

            entity.Property<string>("Name")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.Property<string>("PackageFamilyName")
                .HasColumnType("TEXT");

            entity.Property<AppSource>("Source")
                .HasColumnType("TEXT")
                .HasConversion<string>();

            entity.Property<string>("WorkingDirectory")
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.HasIndex(new[] { "ExecutablePath", "Arguments" }, "IX_Applications_ExecutablePath_Arguments")
                .IsUnique();

            entity.HasIndex("Name", "idx_apps_name");

            entity.ToTable("Applications");
        });

        modelBuilder.Entity<AppTokenEntity>(entity =>
        {
            entity.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            entity.Property<long>("AppId")
                .HasColumnType("INTEGER");

            entity.Property<string>("Token")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.HasIndex("AppId");

            entity.HasIndex("Token", "idx_tokens_token");

            entity.ToTable("AppTokens");
        });

        modelBuilder.Entity<PinEntity>(entity =>
        {
            entity.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            entity.Property<long>("ApplicationId")
                .HasColumnType("INTEGER");

            entity.Property<string>("GroupName")
                .HasColumnType("TEXT");

            entity.Property<int>("Position")
                .HasColumnType("INTEGER");

            entity.HasKey("Id");

            entity.HasIndex("Position", "idx_pins_position");

            entity.ToTable("UserPins");
        });

        modelBuilder.Entity<PreferenceEntity>(entity =>
        {
            entity.Property<string>("Key")
                .HasColumnType("TEXT");

            entity.Property<DateTimeOffset>("UpdatedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<string>("Value")
                .IsRequired()
                .HasColumnType("TEXT");

            entity.HasKey("Key");

            entity.ToTable("Preferences");
        });

        modelBuilder.Entity<RecentLaunchEntity>(entity =>
        {
            entity.Property<long>("Id")
                .ValueGeneratedOnAdd()
                .HasColumnType("INTEGER");

            entity.Property<long>("ApplicationId")
                .HasColumnType("INTEGER");

            entity.Property<DateTimeOffset>("LaunchedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<string>("SearchQuery")
                .HasColumnType("TEXT");

            entity.HasKey("Id");

            entity.HasIndex("ApplicationId", "idx_recent_app");

            entity.ToTable("RecentLaunches");
        });

        modelBuilder.Entity<SchemaVersionEntity>(entity =>
        {
            entity.Property<int>("Id")
                .HasColumnType("INTEGER");

            entity.Property<DateTimeOffset>("UpdatedAtUtc")
                .HasColumnType("TEXT");

            entity.Property<int>("Version")
                .HasColumnType("INTEGER");

            entity.HasKey("Id");

            entity.ToTable("SchemaVersion");
        });

        modelBuilder.Entity<AppTokenEntity>()
            .HasOne<AppEntity>()
            .WithMany("Tokens")
            .HasForeignKey("AppId")
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
