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
    /// Name of the shortcut to <c>/Applications</c> placed inside the staged volume, so the
    /// application installs by dragging. Both ends have to agree on it: it is created as a symlink
    /// and the cleanup has to know it is one, see <see cref="CleanStagingDirectory" />.
    /// </summary>
    private const string ApplicationsShortcutName = "Applications";

    /// <summary>
    /// Ad-hoc sign the published application bundle and wrap it into a compressed disk image.
    /// The bundle is removed afterwards, the image is the only macOS deliverable.
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

        // vpk bundle neither signs nor seals the bundle, only the AOT binary carries the linker's
        // ad-hoc signature. Gatekeeper reports such a downloaded app as "damaged" on Apple Silicon,
        // so seal the whole bundle with an ad-hoc signature. This is not a substitute for a
        // Developer ID signature and notarization, users still have to allow the app once.
        RunOrFail(
            context,
            "codesign",
            new ProcessArgumentBuilder()
                .Append("--force")
                .Append("--deep")
                .Append("--sign").Append("-")
                .AppendQuoted(appPath.FullPath));

        // The volume holds the application and a link to /Applications so it installs by dragging.
        // ditto is the canonical way to copy a bundle on macOS: unlike a plain file copy it keeps
        // the executable bits, the resource forks and the symlinks inside the bundle intact.
        DirectoryPath stagingDirectory = context.DmgStagingDirectory;
        CleanStagingDirectory(context, stagingDirectory);
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
                .AppendQuoted(stagingDirectory.Combine(ApplicationsShortcutName).FullPath));

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

        CleanStagingDirectory(context, stagingDirectory);

        // The bundle now lives in the image. Left next to it, it would be uploaded as a loose
        // directory, and the artifact zip drops the executable bits, which also makes macOS report
        // it as damaged. Directory.Delete removes symlinks inside the bundle without following
        // them, unlike Cake's DeleteDirectory.
        System.IO.Directory.Delete(appPath.FullPath, recursive: true);
        context.Information($"Created {dmgPath}");
        return dmgPath;
    }

    /// <summary>
    /// Empty the staging directory, creating it when it does not exist yet.
    /// </summary>
    /// <param name="context">Build context.</param>
    /// <param name="stagingDirectory">Staging directory to empty.</param>
    /// <remarks>
    /// Plain <c>CleanDirectory</c> must never be pointed at this directory: Cake recurses through
    /// symlinks instead of unlinking them, so it walks through the <c>Applications</c> shortcut into
    /// the real <c>/Applications</c> and deletes whatever is in there. Only file permissions stop it,
    /// which is why this used to surface as "Access to the path ... is denied" on a CI runner and
    /// would have silently wiped user writable applications on a developer machine.
    /// The shortcut is therefore unlinked first - <see cref="System.IO.File.Delete(string)" /> removes
    /// the link itself and never touches its target - and only the plain files that remain go to Cake.
    /// </remarks>
    private static void CleanStagingDirectory(BuildContext context, DirectoryPath stagingDirectory)
    {
        var shortcutPath = stagingDirectory.Combine(ApplicationsShortcutName).FullPath;

        // DirectoryInfo is the only reliable probe here: Cake reports FileExists == false and
        // DirectoryExists == true for a symlink to a directory, so neither alias can tell the
        // shortcut apart from a real directory. LinkTarget is non null for a symlink only, and for
        // a missing path it is null, so a first run on a clean checkout is a plain no-op.
        // Fully qualified because Cake.Core.IO has an internal File type of its own.
        if (new System.IO.DirectoryInfo(shortcutPath).LinkTarget is not null)
        {
            System.IO.File.Delete(shortcutPath);
        }

        // Creates the directory as well when it is missing, which is the case on a clean checkout.
        context.CleanDirectory(stagingDirectory);
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
