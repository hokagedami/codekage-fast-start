namespace FastStart.Data;

/// <summary>
/// Initializes and repairs the FastStart database.
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// Ensures the database is migrated and healthy.
    /// </summary>
    Task InitializeAsync(CancellationToken ct);
}
