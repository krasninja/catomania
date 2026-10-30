using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
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
    /// Velopack release feed: the assets of the latest GitHub release. The release holds the feeds of
    /// every channel side by side (<c>releases.&lt;channel&gt;.json</c>), so one URI serves all platforms.
    /// </summary>
    public const string DefaultUpdateUrl = "https://github.com/krasninja/catomania/releases/latest/download";

    private readonly UpdateManager _updateManager;
    private readonly HttpMessageHandler? _httpMessageHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppUpdateService" /> class.
    /// </summary>
    /// <param name="updateUrl">Release feed URI, <see cref="DefaultUpdateUrl" /> when omitted.</param>
    /// <param name="locator">Velopack locator, the application one when omitted. Used by tests.</param>
    /// <param name="httpMessageHandler">HTTP handler used to read the release feed, the default one when omitted. Used by tests.</param>
    public AppUpdateService(
        string? updateUrl = null,
        IVelopackLocator? locator = null,
        HttpMessageHandler? httpMessageHandler = null)
    {
        UpdateUrl = updateUrl ?? DefaultUpdateUrl;
        _updateManager = new UpdateManager(UpdateUrl, null, locator);
        _httpMessageHandler = httpMessageHandler;
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
    /// Get the Velopack channel of the current platform, it names the feed file <c>releases.&lt;channel&gt;.json</c>.
    /// </summary>
    /// <returns>Platform identifier.</returns>
    public static string GetPlatform()
    {
        var platform = OperatingSystem.IsWindows() ? "win"
            : OperatingSystem.IsMacOS() ? "osx"
            : "linux";

        // x64 keeps the plain platform name, so the feeds published before the arm64 builds existed
        // stay valid and nobody is pointed at an empty channel. Every other architecture gets its
        // own one, matching the channel the build script publishes to. An x64 binary running under
        // Rosetta or any other emulation reports X64 here, which is what it needs.
        return RuntimeInformation.ProcessArchitecture == Architecture.X64
            ? platform
            : $"{platform}-{GetArchitecture()}";
    }

    /// <summary>
    /// Get the name of the current process architecture as the build script and Velopack spell it.
    /// </summary>
    /// <returns>Architecture name.</returns>
    private static string GetArchitecture() => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.Arm64 => "arm64",
        Architecture.Arm => "arm",
        Architecture.X86 => "x86",
        _ => RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(),
    };

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
        using var httpClient = _httpMessageHandler != null
            ? new HttpClient(_httpMessageHandler, disposeHandler: false)
            : new HttpClient();
        foreach (var indexName in new[] { $"releases.{GetPlatform()}.json", "releases.json" })
        {
            using var response = await httpClient
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
    /// <param name="restartAfterUpdate">Whether to start the application again after the update is applied.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Update check result.</returns>
    public async Task<UpdateReport> CheckAndUpdateAsync(
        bool restartAfterUpdate = false,
        CancellationToken cancellationToken = default)
    {
        var current = GetCurrentVersion();

        SemanticVersion? latest;
        try
        {
            latest = await GetLatestVersionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or JsonException)
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
            cancellationToken.ThrowIfCancellationRequested();
            var updateInfo = await _updateManager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (updateInfo is null)
            {
                return new UpdateReport(UpdateReportKind.UpToDate, $"Catomania {current} is up to date.");
            }

            await _updateManager.DownloadUpdatesAsync(updateInfo, cancelToken: cancellationToken).ConfigureAwait(false);
            // Both calls exit the process.
            if (restartAfterUpdate)
            {
                _updateManager.ApplyUpdatesAndRestart(updateInfo);
            }
            else
            {
                _updateManager.ApplyUpdatesAndExit(updateInfo);
            }

            return new UpdateReport(
                UpdateReportKind.Updating,
                $"Updating Catomania to {updateInfo.TargetFullRelease.Version}.");
        }
        catch (Exception e)
        {
            return new UpdateReport(UpdateReportKind.Error, $"Failed to apply update {latest}: {e.Message}");
        }
    }
}
