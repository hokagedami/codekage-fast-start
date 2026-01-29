namespace FastStart.Data;

/// <summary>
/// Provides paths for database storage.
/// </summary>
public interface IDbPathProvider
{
    /// <summary>
    /// Gets the base directory for FastStart data.
    /// </summary>
    string BaseDirectory { get; }

    /// <summary>
    /// Gets the SQLite database path.
    /// </summary>
    string DatabasePath { get; }
}
