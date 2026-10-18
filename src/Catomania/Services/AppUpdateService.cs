using System.Net.Http;
using System.Reflection;
using Catomania.Models;
using Velopack;
using Velopack.Locators;

namespace Catomania.Services;

/// <summary>
/// Checks the current application version and applies updates from the Velopack release feed.
/// </summary>
public sealed class AppUpdateService
{
    /// <summary>
    /// Base URI with the Velopack releases per platform.
    /// </summary>
    public const string BaseUpdateUrl = "https://misc.anti-soft.ru/catomania";

    private readonly UpdateManager _updateManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppUpdateService" /> class.
    /// </summary>
    /// <param name="updateUrl">Release feed URI, the platform default one when omitted.</param>
    /// <param name="locator">Velopack locator, the application one when omitted. Used by tests.</param>
    public AppUpdateService(string? updateUrl = null, IVelopackLocator? locator = null)
    {
        UpdateUrl = updateUrl ?? GetDefaultUpdateUrl();
        _updateManager = new UpdateManager(UpdateUrl, null, locator);
    }

    /// <summary>
    /// Gets the release feed URI.
    /// </summary>
    public string UpdateUrl { get; }

    /// <summary>
    /// Gets a value indicating whether the application is installed by Velopack and can update itself.
    /// </summary>
    public bool IsInstalled => _updateManager.IsInstalled;

    /// <summary>
    /// Get the default release feed URI for the current platform.
    /// </summary>
    /// <returns>Release feed URI.</returns>
    public static string GetDefaultUpdateUrl() => $"{BaseUpdateUrl}/{GetPlatform()}";

    /// <summary>
    /// Get the current platform identifier used in the release feed URI.
    /// </summary>
    /// <returns>Platform identifier.</returns>
    public static string GetPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return "win";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "osx";
        }

        return "linux";
    }

    /// <summary>
    /// Get the current application version: the installed one when the application
    /// is installed by Velopack, the assembly version otherwise.
    /// </summary>
    /// <returns>Current version.</returns>
    public string GetCurrentVersion()
        => IsInstalled
            ? _updateManager.CurrentVersion?.ToString() ?? string.Empty
            : GetAssemblyVersion();

    /// <summary>
    /// Get the application version the assembly was built with, without the commit hash suffix.
    /// </summary>
    /// <returns>Assembly version.</returns>
    public static string GetAssemblyVersion()
    {
        var version = typeof(AppUpdateService).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        var hashSeparatorIndex = version.IndexOf('+', StringComparison.Ordinal);
        return hashSeparatorIndex > -1 ? version.Substring(0, hashSeparatorIndex) : version;
    }

    /// <summary>
    /// Get the latest version published to the release feed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The latest published version or null when the feed has no releases yet.</returns>
    public async Task<SemanticVersion?> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();
        foreach (var indexName in new[] { $"releases.{GetPlatform()}.json", "releases.json" })
        {
            var response = await httpClient
                .GetAsync($"{UpdateUrl.TrimEnd('/')}/{indexName}", cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                continue;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var feed = VelopackAssetFeed.FromJson(json);
            return feed.Assets
                .Where(asset => asset.Type == VelopackAssetType.Full)
                .Select(asset => asset.Version)
                .OrderByDescending(version => version)
                .FirstOrDefault();
        }

        return null;
    }

    /// <summary>
    /// Check the release feed for a newer version and apply it when the application is installed by Velopack.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Update check result.</returns>
    public async Task<UpdateReport> CheckAndUpdateAsync(CancellationToken cancellationToken = default)
    {
        var current = GetCurrentVersion();

        SemanticVersion? latest;
        try
        {
            latest = await GetLatestVersionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return new UpdateReport(UpdateReportKind.Error, $"Failed to check updates at {UpdateUrl}: {e.Message}");
        }

        if (latest is null)
        {
            return new UpdateReport(
                UpdateReportKind.NoReleases,
                $"No releases are published at {UpdateUrl} yet. Current version: {current}.");
        }

        if (!SemanticVersion.TryParse(current, out var currentVersion) || currentVersion >= latest)
        {
            return new UpdateReport(UpdateReportKind.UpToDate, $"Catomania {current} is up to date.");
        }

        if (!IsInstalled)
        {
            return new UpdateReport(
                UpdateReportKind.NotInstalled,
                $"Version {latest} is available, but this build is not installed by Velopack and cannot update itself. Current version: {current}.");
        }

        try
        {
            var updateInfo = await _updateManager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (updateInfo is null)
            {
                return new UpdateReport(UpdateReportKind.UpToDate, $"Catomania {current} is up to date.");
            }

            await _updateManager.DownloadUpdatesAsync(updateInfo, cancelToken: cancellationToken).ConfigureAwait(false);
            _updateManager.ApplyUpdatesAndExit(updateInfo);
            return new UpdateReport(
                UpdateReportKind.Updating,
                $"Updating Catomania to {updateInfo.TargetFullRelease.Version}, the application will restart.");
        }
        catch (Exception e)
        {
            return new UpdateReport(UpdateReportKind.Error, $"Failed to apply update {latest}: {e.Message}");
        }
    }
}
