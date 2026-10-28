using Cake.Common;
using Cake.Common.IO;
using Cake.Common.IO.Paths;
using Cake.Common.Tools.GitVersion;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;

namespace Catomania.Build;

/// <summary>
/// Cake Frosting build context.
/// </summary>
public class BuildContext : FrostingContext
{
    private const string FallbackVersion = "0.1.0";

    /// <summary>
    /// Directory with published binaries. It is cleaned before every publish, so it must not contain
    /// the releases: Velopack needs the previous ones to build delta packages and the full feed.
    /// </summary>
    public ConvertableDirectoryPath OutputDirectory => this.Directory("./output/publish");

    /// <summary>
    /// Directory with Velopack releases.
    /// </summary>
    public ConvertableDirectoryPath ReleasesDirectory => this.Directory("./output/releases");

    /// <summary>
    /// Temporary directory the contents of the macOS disk image are staged in. It must not live
    /// inside <see cref="OutputDirectory" />: Velopack packs everything it finds there.
    /// </summary>
    public ConvertableDirectoryPath DmgStagingDirectory => this.Directory("./output/dmg-staging");

    /// <summary>
    /// Files excluded from the Velopack package. Passing <c>--exclude</c> replaces the Velopack
    /// default of <c>.*\.pdb</c>, so every format has to be listed. Native AOT writes the debug
    /// information next to the binary in a platform specific format instead of a portable pdb, and
    /// those files are tens of megabytes that must not reach the users.
    /// </summary>
    internal const string VelopackExcludeRegex = @".*\.(pdb|dbg)$|.*\.dSYM[/\\].*";

    /// <summary>
    /// Application version to publish.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Target CPU architecture, x64 by default. Overridden with the <c>--arch</c> argument or the
    /// <c>CATOMANIA_ARCH</c> environment variable. Native AOT cannot cross compile architectures,
    /// so every architecture has to be built on a matching machine.
    /// </summary>
    public string Architecture { get; }

    /// <inheritdoc />
    public BuildContext(ICakeContext context)
        : base(context)
    {
        context.EnsureDirectoryExists(OutputDirectory);

        // "--version" cannot be used here: Spectre.Console reserves it for the build tool's own
        // version flag. The environment variable is the documented way to override it.
        // Empty values are ignored, CI sets the variables unconditionally and an empty one has to
        // fall through to the next source instead of producing an empty version.
        Version = FirstNonEmpty(
                      context.Arguments.GetArgument("appversion"),
                      Environment.GetEnvironmentVariable("CATOMANIA_VERSION"))
                  ?? GetGitVersion(context);
        Architecture = FirstNonEmpty(
                           context.Arguments.GetArgument("arch"),
                           Environment.GetEnvironmentVariable("CATOMANIA_ARCH"))
                       ?? ApplicationConstants.ArchitectureX64;
    }

    /// <summary>
    /// Get the first value that is not null, empty or whitespace.
    /// </summary>
    /// <param name="values">Candidate values in the order of precedence.</param>
    /// <returns>The first usable value trimmed, or <c>null</c> when there is none.</returns>
    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// Get the Velopack release channel of a platform. It is also the name of the subdirectory the
    /// releases are written to. x64 keeps the plain platform name, so the feeds already published at
    /// <c>https://catomania.anti-soft.ru/&lt;platform&gt;</c> stay valid, other architectures get their own
    /// channel to keep the feeds separate.
    /// </summary>
    /// <param name="platform">Platform identifier.</param>
    /// <returns>Release channel.</returns>
    internal string GetChannel(string platform)
        => Architecture == ApplicationConstants.ArchitectureX64
            ? platform
            : $"{platform}-{Architecture}";

    /// <summary>
    /// Get the .NET runtime identifier of a platform for the current architecture.
    /// </summary>
    /// <param name="platform">Platform identifier.</param>
    /// <returns>Runtime identifier.</returns>
    internal string GetRuntimeIdentifier(string platform) => $"{platform}-{Architecture}";

    /// <summary>
    /// Build Velopack (vpk) command line arguments for the platform.
    /// </summary>
    /// <param name="platform">Platform identifier.</param>
    /// <param name="arch">Architecture identifier, the context one when omitted.</param>
    /// <returns>Velopack arguments.</returns>
    internal string[] GetVelopackArguments(string platform, string? arch = null)
    {
        arch ??= Architecture;
        var channel = GetChannel(platform);

        // https://docs.velopack.io/packaging/cross-compiling.
        // macOS packages use the "bundle" command; other platforms use "pack".
        var command = platform == ApplicationConstants.PlatformMacOS ? "bundle" : "pack";
        var iconPath = platform == ApplicationConstants.PlatformMacOS
            ? "./src/Catomania/Assets/Icon.icns"
            : "./src/Catomania/Assets/Icon.ico";
        return new[]
        {
            $"[{platform}]", command,
            "--runtime", $"{platform}-{arch}",
            "--packId", ApplicationConstants.PackId,
            "--packVersion", Version,
            "--packTitle", ApplicationConstants.ProductName,
            "--packAuthors", "\"Ivan Kozhin\"",
            "--icon", iconPath,
            "--packDir", OutputDirectory,
            "--exclude", VelopackExcludeRegex,
            "--channel", channel,
            "--mainExe", platform.Contains("win", StringComparison.InvariantCultureIgnoreCase)
                ? $"{ApplicationConstants.MainExeName}.exe"
                : ApplicationConstants.MainExeName,
            "--outputDir", Path.Combine(ReleasesDirectory, channel),
            "-xy",
        };
    }

    /// <summary>
    /// Run the Velopack CLI (vpk) with the arguments and fail the build on a non-zero exit code.
    /// </summary>
    /// <param name="arguments">Velopack arguments.</param>
    internal void RunVelopack(string[] arguments)
    {
        var exitCode = this.StartProcess("vpk", string.Join(' ', arguments));
        if (exitCode != 0)
        {
            throw new CakeException($"Velopack packaging failed with exit code {exitCode}.");
        }
    }

    private string GetGitVersion(ICakeContext context)
    {
        try
        {
            return context.GitVersion().SemVer;
        }
        catch (Exception e)
        {
            context.Log.Write(Verbosity.Normal, LogLevel.Warning, $"Failed to get the version from git: {e.Message}");
            return FallbackVersion;
        }
    }
}
