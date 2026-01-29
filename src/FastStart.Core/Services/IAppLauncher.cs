using System.Threading;
using System.Threading.Tasks;
using FastStart.Core.Models;

namespace FastStart.Core.Services;

public interface IAppLauncher
{
    Task<bool> LaunchAsync(AppInfo app, string? query, CancellationToken ct);
}