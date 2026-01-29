using System.Runtime.CompilerServices;
using FastStart.Core.Models;
using FastStart.Core.Services;

namespace FastStart.Native;

/// <summary>
/// Placeholder UWP enumerator until WinRT integration is implemented.
/// </summary>
public sealed class StubUwpAppEnumerator : IUwpAppEnumerator
{
    /// <inheritdoc />
    public async IAsyncEnumerable<AppInfo> EnumerateAsync([EnumeratorCancellation] CancellationToken ct)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }
}
