using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;

namespace Catomania.Build;

/// <summary>
/// Creates the macOS disk image from the published application bundle.
/// Velopack only produces the <c>.app</c> bundle, the <c>.dmg</c> is the format macOS users expect
/// to download: it opens as a volume with the application and a shortcut to <c>/Applications</c>.
/// </summary>
/// <remarks>
/// This is a plain helper rather than a Cake task dependency on purpose. <c>hdiutil</c> only exists
/// on macOS, and a task skipped by <c>ShouldRun</c> makes Cake fail every task that depends on it,
/// which would take <c>Publish-Mac</c> down with it.
/// </remarks>
public static class MacDiskImage
{
    /// <summary>
    /// Wrap the published application bundle into a compressed disk image.
    /// Does nothing with a message when not running on macOS.
    /// </summary>
    /// <param name="context">Build context.</param>
    /// <returns>The path of the created image, or <c>null</c> when it was not created.</returns>
    public static FilePath? Create(BuildContext context)
    {
        if (!context.IsRunningOnMacOs())
        {
            context.Information("Skipping the .dmg creation: hdiutil is only available on macOS.");
            return null;
        }

        var channel = context.GetChannel(ApplicationConstants.PlatformMacOS);

        // ConvertableDirectoryPath only converts implicitly, Combine lives on DirectoryPath.
        DirectoryPath releasesDirectory = context.ReleasesDirectory;
        var channelDirectory = releasesDirectory.Combine(channel);
        var appPath = channelDirectory.Combine($"{ApplicationConstants.PackId}.app");
        if (!context.DirectoryExists(appPath))
        {
            throw new CakeException($"The application bundle {appPath} is missing, run Publish-Mac first.");
        }

        // The volume holds the application and a link to /Applications so it installs by dragging.
        // ditto is the canonical way to copy a bundle on macOS: unlike a plain file copy it keeps
        // the executable bits, the resource forks and the symlinks inside the bundle intact.
        DirectoryPath stagingDirectory = context.DmgStagingDirectory;
        context.CleanDirectory(stagingDirectory);
        var stagedAppPath = stagingDirectory.Combine($"{ApplicationConstants.PackId}.app");
        RunOrFail(
            context,
            "ditto",
            new ProcessArgumentBuilder()
                .AppendQuoted(appPath.FullPath)
                .AppendQuoted(stagedAppPath.FullPath));
        RunOrFail(
            context,
            "ln",
            new ProcessArgumentBuilder()
                .Append("-s")
                .Append("/Applications")
                .AppendQuoted(stagingDirectory.Combine("Applications").FullPath));

        var dmgPath = channelDirectory
            .CombineWithFilePath($"{ApplicationConstants.PackId}-{context.Version}-{channel}.dmg");

        // Cake's DeleteFile throws a FileNotFoundException instead of ignoring a missing file, and
        // on a clean checkout - which is always the case in CI - no image has been created yet.
        // The delete only guards against a stale image of the same version, hdiutil is passed -ov
        // below and overwrites whatever is there anyway.
        if (context.FileExists(dmgPath))
        {
            context.DeleteFile(dmgPath);
        }

        // UDZO is a compressed, read only image, the usual format for distribution.
        RunOrFail(
            context,
            "hdiutil",
            new ProcessArgumentBuilder()
                .Append("create")
                .Append("-volname").AppendQuoted(ApplicationConstants.ProductName)
                .Append("-srcfolder").AppendQuoted(stagingDirectory.FullPath)
                .Append("-ov")
                .Append("-format").Append("UDZO")
                .AppendQuoted(dmgPath.FullPath));

        context.CleanDirectory(stagingDirectory);
        context.Information($"Created {dmgPath}");
        return dmgPath;
    }

    private static void RunOrFail(BuildContext context, string tool, ProcessArgumentBuilder arguments)
    {
        var exitCode = context.StartProcess(tool, new ProcessSettings { Arguments = arguments });
        if (exitCode != 0)
        {
            throw new CakeException($"{tool} failed with exit code {exitCode}.");
        }
    }
}
