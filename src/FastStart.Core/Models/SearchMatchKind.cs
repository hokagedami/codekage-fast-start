namespace FastStart.Core.Models;

/// <summary>
/// Describes how a search term matched an application.
/// </summary>
public enum SearchMatchKind
{
    None = 0,
    Exact = 1,
    Prefix = 2,
    Substring = 3,
    Fuzzy = 4
}
