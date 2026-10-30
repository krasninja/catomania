using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Catomania.Models;
using Catomania.Services;
using Velopack;
using Velopack.Locators;
using Xunit;

namespace Catomania.Tests.Services;

/// <summary>
/// Tests for <see cref="AppUpdateService" />. The release feed is served by a local HTTP listener.
/// </summary>
public sealed class AppUpdateServiceTests
{
    static AppUpdateServiceTests()
    {
        // Sets the Velopack locator of the process: not installed outside an AppImage.
        VelopackApp.Build().Run();
    }

    [Fact]
    public void UpdateUrl_DefaultsToTheLatestGitHubRelease()
    {
        Assert.Equal(AppUpdateService.DefaultUpdateUrl, new AppUpdateService().UpdateUrl);
    }

    [Fact]
    public void GetPlatform_QualifiesTheChannelWithTheArchitectureExceptOnX64()
    {
        var platform = AppUpdateService.GetPlatform();

        // The channel has to start with the platform the build script publishes to, otherwise the
        // application reads a feed that nobody writes.
        var expectedPrefix = OperatingSystem.IsWindows() ? "win"
            : OperatingSystem.IsMacOS() ? "osx"
            : "linux";
        Assert.StartsWith(expectedPrefix, platform, StringComparison.Ordinal);

        // x64 keeps the plain platform name so the feeds published before the arm64 builds existed
        // stay valid. Every other architecture gets its own channel, matching BuildContext.GetChannel.
        var expected = RuntimeInformation.ProcessArchitecture == Architecture.X64
            ? expectedPrefix
            : $"{expectedPrefix}-{RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()}";
        Assert.Equal(expected, platform);
    }

    [Fact]
    public void GetAssemblyVersion_ReturnsVersionWithoutCommitHash()
    {
        var version = AppUpdateService.GetAssemblyVersion();

        Assert.NotEmpty(version);
        Assert.DoesNotContain("+", version);
        Assert.True(SemanticVersion.TryParse(version, out _), $"'{version}' is not a semantic version");
    }

    [Fact]
    public async Task GetLatestVersionAsync_ReadsTheReleaseFeed()
    {
        await using var feed = new FeedServer(new Dictionary<string, string>
        {
            [$"releases.{AppUpdateService.GetPlatform()}.json"] = CreateFeedJson("99.1.2"),
        });

        var latest = await CreateService(feed.Url).GetLatestVersionAsync();

        Assert.NotNull(latest);
        Assert.Equal("99.1.2", latest!.ToString());
    }

    [Fact]
    public async Task CheckAndUpdateAsync_EmptyFeed_ReportsNoReleases()
    {
        await using var feed = new FeedServer(new Dictionary<string, string>());

        var report = await CreateService(feed.Url).CheckAndUpdateAsync();

        Assert.Equal(UpdateReportKind.NoReleases, report.Kind);
    }

    [Fact]
    public async Task CheckAndUpdateAsync_SameVersion_ReportsUpToDate()
    {
        await using var feed = new FeedServer(new Dictionary<string, string>
        {
            [$"releases.{AppUpdateService.GetPlatform()}.json"] = CreateFeedJson(AppUpdateService.GetAssemblyVersion()),
        });

        var report = await CreateService(feed.Url).CheckAndUpdateAsync();

        Assert.Equal(UpdateReportKind.UpToDate, report.Kind);
    }

    [Fact]
    public async Task CheckAndUpdateAsync_NewerVersionWhileNotInstalled_ReportsNotInstalled()
    {
        await using var feed = new FeedServer(new Dictionary<string, string>
        {
            [$"releases.{AppUpdateService.GetPlatform()}.json"] = CreateFeedJson("99.0.0"),
        });
        var service = CreateService(feed.Url);

        Assert.False(service.IsInstalled);
        var report = await service.CheckAndUpdateAsync();

        Assert.Equal(UpdateReportKind.NotInstalled, report.Kind);
        Assert.Contains("99.0.0", report.Message);
    }

    [Fact]
    public void InstalledLocator_ReportsTheInstalledVersion()
    {
        var service = new AppUpdateService(
            "https://example.invalid/catomania/linux",
            new TestVelopackLocator("catomania", "1.2.3", Path.GetTempPath()));

        Assert.True(service.IsInstalled);
        Assert.Equal("1.2.3", service.GetCurrentVersion());
    }

    [Fact]
    public async Task CheckAndUpdateAsync_InstalledAndSameVersion_ReportsUpToDate()
    {
        await using var feed = new FeedServer(new Dictionary<string, string>
        {
            [$"releases.{AppUpdateService.GetPlatform()}.json"] = CreateFeedJson("1.2.3"),
        });
        var service = CreateService(
            feed.Url,
            new TestVelopackLocator("catomania", "1.2.3", Path.GetTempPath()));

        var report = await service.CheckAndUpdateAsync();

        Assert.Equal(UpdateReportKind.UpToDate, report.Kind);
    }

    [Fact]
    public async Task CheckAndUpdateAsync_InvalidFeed_ReportsError()
    {
        // A captive portal or a misconfigured server returns HTML instead of the feed.
        await using var feed = new FeedServer(new Dictionary<string, string>
        {
            [$"releases.{AppUpdateService.GetPlatform()}.json"] = "<html>Sign in to the network</html>",
        });

        var report = await CreateService(feed.Url).CheckAndUpdateAsync();

        Assert.Equal(UpdateReportKind.Error, report.Kind);
    }

    /// <summary>
    /// Create the service that reads the local feed directly, ignoring the proxy settings of the environment.
    /// </summary>
    private static AppUpdateService CreateService(string url, IVelopackLocator? locator = null)
        => new(url, locator, new HttpClientHandler { UseProxy = false });

    private static string CreateFeedJson(string version)
        => "{\"Assets\":[{\"PackageId\":\"catomania\",\"Version\":\"" + version + "\","
           + "\"Type\":\"Full\",\"FileName\":\"catomania-" + version + "-full.nupkg\","
           + "\"SHA1\":\"6AD64C5174E43C84739061F12F77DDEF2CE0B5E7\","
           + "\"SHA256\":\"8F03B08A5E92A75579D64AA081222B5074C931B21BB8D989C73D83BC67E84CE3\",\"Size\":1}]}";

    private sealed class FeedServer : IAsyncDisposable
    {
        private readonly HttpListener _listener = new();

        public FeedServer(IReadOnlyDictionary<string, string> files)
        {
            var port = GetFreePort();
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();
            Url = $"http://127.0.0.1:{port}";
            _ = Task.Run(() => ServeAsync(files));
        }

        public string Url { get; }

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();
            _listener.Close();
            await Task.CompletedTask;
        }

        private static int GetFreePort()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            var port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
            tcpListener.Stop();
            return port;
        }

        private async Task ServeAsync(IReadOnlyDictionary<string, string> files)
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }

                var path = context.Request.Url?.AbsolutePath.TrimStart('/') ?? string.Empty;
                var body = files.TryGetValue(path, out var content)
                    ? Encoding.UTF8.GetBytes(content)
                    : [];
                context.Response.StatusCode = body.Length > 0 ? 200 : 404;
                context.Response.ContentLength64 = body.Length;
                await context.Response.OutputStream.WriteAsync(body);
                context.Response.Close();
            }
        }
    }
}
