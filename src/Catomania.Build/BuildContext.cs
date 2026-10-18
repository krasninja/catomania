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
    /// Directory with published binaries.
    /// </summary>
    public ConvertableDirectoryPath OutputDirectory => this.Directory("./output");

    /// <summary>
    /// Directory with Velopack releases.
    /// </summary>
    public ConvertableDirectoryPath ReleasesDirectory => this.Directory("./output/releases");

    /// <summary>
    /// Application version to publish.
    /// </summary>
    public string Version { get; }

    /// <inheritdoc />
    public BuildContext(ICakeContext context)
        : base(context)
    {
        context.EnsureDirectoryExists(OutputDirectory);
        Version = context.Arguments.GetArgument("version")
                  ?? Environment.GetEnvironmentVariable("CATOMANIA_VERSION")
                  ?? GetGitVersion(context);
    }

    /// <summary>
    /// Build Velopack (vpk) command line arguments for the platform.
    /// </summary>
    /// <param name="platform">Platform identifier.</param>
    /// <param name="arch">Architecture identifier.</param>
    /// <returns>Velopack arguments.</returns>
    internal string[] GetVelopackArguments(string platform, string arch = "x64")
    {
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
            "--mainExe", platform.Contains("win", StringComparison.InvariantCultureIgnoreCase)
                ? $"{ApplicationConstants.MainExeName}.exe"
                : ApplicationConstants.MainExeName,
            "--outputDir", Path.Combine(ReleasesDirectory, platform),
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
