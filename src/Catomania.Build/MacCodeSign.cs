using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;

// Cake.Core.IO has a Path type of its own, the file system one is meant here.
using Path = System.IO.Path;

namespace Catomania.Build;

/// <summary>
/// Signs the macOS application bundle and disk image with a Developer ID Application
/// certificate and optionally notarizes them with Apple. Velopack produces an unsigned bundle
/// and the ad-hoc fallback in <see cref="MacDiskImage" /> is rejected by Gatekeeper for
/// downloaded apps, so a real signature is the only way to a build users can just open.
/// </summary>
/// <remarks>
/// <para>
/// Configured entirely through environment variables so the same code path serves local
/// builds and CI:
/// </para>
/// <list type="bullet">
/// <item><c>CATOMANIA_MAC_CERT_FILE</c> - path to the exported .p12 certificate (local builds).</item>
/// <item><c>CATOMANIA_MAC_CERT_BASE64</c> - base64 of the same .p12 (CI secret), used when no file is set.</item>
/// <item><c>CATOMANIA_MAC_CERT_PASSWORD</c> - the .p12 export password, may be empty.</item>
/// <item><c>CATOMANIA_MAC_SIGN_IDENTITY</c> - optional identity override, detected from the certificate when omitted.</item>
/// <item><c>CATOMANIA_MAC_NOTARY_APPLE_ID</c>, <c>CATOMANIA_MAC_NOTARY_PASSWORD</c> (an app-specific
/// password) and <c>CATOMANIA_MAC_NOTARY_TEAM_ID</c> - enable notarization when all three are set.</item>
/// </list>
/// <para>
/// Like <see cref="MacDiskImage" /> this is a plain helper rather than a Cake task on purpose:
/// <c>security</c>, <c>codesign</c> and <c>xcrun</c> only exist on macOS, and a task skipped by
/// <c>ShouldRun</c> would take every dependent task down with it.
/// </para>
/// </remarks>
public sealed class MacCodeSign : IDisposable
{
    private const string DeveloperIdPrefix = "Developer ID Application";

    private readonly BuildContext _context;
    private readonly string _keychainPath;
    private readonly string _keychainPassword;
    private readonly string _identity;
    private readonly string? _temporaryCertificateFile;
    private readonly string[] _originalKeychains;
    private readonly NotaryCredentials? _notary;

    private MacCodeSign(BuildContext context, string certificateFile, string? temporaryCertificateFile)
    {
        _context = context;
        _temporaryCertificateFile = temporaryCertificateFile;
        _notary = NotaryCredentials.TryRead(context);
        _keychainPassword = Guid.NewGuid().ToString("N");
        _keychainPath = Path.Combine(Path.GetTempPath(), $"catomania-sign-{_keychainPassword}.keychain-db");
        _originalKeychains = GetKeychainSearchList();

        try
        {
            // A dedicated temporary keychain keeps the certificate out of the user's login
            // keychain and lets a headless CI runner import it without any GUI prompt.
            RunOrFail(
                "security",
                new ProcessArgumentBuilder()
                    .Append("create-keychain")
                    .Append("-p").AppendSecret(_keychainPassword)
                    .AppendQuoted(_keychainPath));

            // No -t/-l: the keychain must not auto-lock during a long notarization wait.
            RunOrFail(
                "security",
                new ProcessArgumentBuilder()
                    .Append("set-keychain-settings").AppendQuoted(_keychainPath));
            RunOrFail(
                "security",
                new ProcessArgumentBuilder()
                    .Append("unlock-keychain")
                    .Append("-p").AppendSecret(_keychainPassword)
                    .AppendQuoted(_keychainPath));

            // codesign only finds identities in keychains on the user search list. The original
            // list is restored in Dispose, the login keychain stays the default one.
            var searchList = new ProcessArgumentBuilder()
                .Append("list-keychains").Append("-d").Append("user").Append("-s");
            foreach (var keychain in _originalKeychains)
            {
                searchList.AppendQuoted(keychain);
            }

            searchList.AppendQuoted(_keychainPath);
            RunOrFail("security", searchList);

            var certificatePassword = Environment.GetEnvironmentVariable("CATOMANIA_MAC_CERT_PASSWORD") ?? string.Empty;
            RunOrFail(
                "security",
                new ProcessArgumentBuilder()
                    .Append("import").AppendQuoted(certificateFile)
                    .Append("-k").AppendQuoted(_keychainPath)
                    .Append("-P").AppendSecret(certificatePassword)
                    .Append("-T").Append("/usr/bin/codesign")
                    .Append("-T").Append("/usr/bin/security"));

            // Without the partition list macOS pops an "allow access" dialog for every
            // signature, which hangs a headless CI runner forever.
            RunOrFail(
                "security",
                new ProcessArgumentBuilder()
                    .Append("set-key-partition-list")
                    .Append("-S").Append("apple-tool:,apple:,codesign:")
                    .Append("-s")
                    .Append("-k").AppendSecret(_keychainPassword)
                    .AppendQuoted(_keychainPath));

            _identity = FirstNonEmpty(Environment.GetEnvironmentVariable("CATOMANIA_MAC_SIGN_IDENTITY"))
                ?? DetectIdentity();
            context.Information($"Signing macOS builds as \"{_identity}\".");
        }
        catch
        {
            CleanupKeychain();
            throw;
        }
    }

    /// <summary>
    /// Start a signing session when a certificate is configured, or return <c>null</c> when
    /// neither <c>CATOMANIA_MAC_CERT_FILE</c> nor <c>CATOMANIA_MAC_CERT_BASE64</c> is set.
    /// Dispose the session to remove the temporary keychain and certificate file.
    /// </summary>
    /// <param name="context">Build context.</param>
    /// <returns>The signing session, or <c>null</c> when signing is not configured.</returns>
    public static MacCodeSign? TryStart(BuildContext context)
    {
        var certificateFile = FirstNonEmpty(Environment.GetEnvironmentVariable("CATOMANIA_MAC_CERT_FILE"));
        var certificateBase64 = FirstNonEmpty(Environment.GetEnvironmentVariable("CATOMANIA_MAC_CERT_BASE64"));
        if (certificateFile is null && certificateBase64 is null)
        {
            return null;
        }

        string? temporaryFile = null;
        if (certificateFile is null)
        {
            temporaryFile = Path.Combine(Path.GetTempPath(), $"catomania-cert-{Guid.NewGuid():N}.p12");
            System.IO.File.WriteAllBytes(temporaryFile, Convert.FromBase64String(certificateBase64!));
            certificateFile = temporaryFile;
        }

        if (!System.IO.File.Exists(certificateFile))
        {
            throw new CakeException($"The macOS signing certificate {certificateFile} does not exist.");
        }

        try
        {
            return new MacCodeSign(context, certificateFile, temporaryFile);
        }
        catch
        {
            if (temporaryFile is not null)
            {
                System.IO.File.Delete(temporaryFile);
            }

            throw;
        }
    }

    /// <summary>
    /// Sign the application bundle with the Developer ID identity, the hardened runtime and a
    /// secure timestamp, then verify the result.
    /// </summary>
    /// <param name="appPath">Path of the .app bundle.</param>
    public void SignBundle(DirectoryPath appPath)
    {
        // Sign inside out: every nested dylib first, then the bundle itself. Apple discourages
        // codesign --deep for Developer ID signing because it mishandles nested code.
        foreach (var library in _context.GetFiles($"{appPath.FullPath}/**/*.dylib"))
        {
            Sign(library.FullPath, hardenedRuntime: true);
        }

        Sign(appPath.FullPath, hardenedRuntime: true);
        RunOrFail(
            "codesign",
            new ProcessArgumentBuilder()
                .Append("--verify").Append("--deep").Append("--strict").Append("--verbose=2")
                .AppendQuoted(appPath.FullPath));
    }

    /// <summary>
    /// Sign a plain file, the disk image, with the Developer ID identity.
    /// </summary>
    /// <param name="path">File to sign.</param>
    public void SignFile(FilePath path) => Sign(path.FullPath, hardenedRuntime: false);

    /// <summary>
    /// Submit the bundle or disk image to Apple's notary service, wait for the verdict and
    /// staple the ticket, so Gatekeeper accepts the download even offline. A no-op with a
    /// message when the <c>CATOMANIA_MAC_NOTARY_*</c> variables are not configured.
    /// </summary>
    /// <param name="path">Path of the .app bundle or the .dmg file.</param>
    public void NotarizeAndStaple(string path)
    {
        if (_notary is null)
        {
            _context.Information($"Skipping the notarization of {path}: CATOMANIA_MAC_NOTARY_* are not configured.");
            return;
        }

        var submissionPath = path;
        string? zipPath = null;
        if (System.IO.Directory.Exists(path))
        {
            // notarytool only accepts bundles as zip archives, and ditto is the canonical way
            // to archive one: it keeps the executable bits, resource forks and symlinks intact.
            zipPath = Path.Combine(Path.GetTempPath(), $"catomania-notary-{Guid.NewGuid():N}.zip");
            RunOrFail(
                "ditto",
                new ProcessArgumentBuilder()
                    .Append("-c").Append("-k").Append("--keepParent")
                    .AppendQuoted(path).AppendQuoted(zipPath));
            submissionPath = zipPath;
        }

        try
        {
            var exitCode = RunWithOutput(
                "xcrun",
                new ProcessArgumentBuilder()
                    .Append("notarytool").Append("submit").AppendQuoted(submissionPath)
                    .Append("--apple-id").AppendQuoted(_notary.AppleId)
                    .Append("--password").AppendSecret(_notary.Password)
                    .Append("--team-id").AppendQuoted(_notary.TeamId)
                    .Append("--wait"),
                out var output);
            foreach (var line in output)
            {
                _context.Information(line);
            }

            // --wait exits non-zero on a rejected submission, the status check guards against
            // a notarytool version that reports "Invalid" with a zero exit code.
            if (exitCode != 0 || !output.Any(line => line.Contains("status: Accepted", StringComparison.Ordinal)))
            {
                throw new CakeException($"The notarization of {path} did not succeed.");
            }
        }
        finally
        {
            if (zipPath is not null)
            {
                System.IO.File.Delete(zipPath);
            }
        }

        RunOrFail(
            "xcrun",
            new ProcessArgumentBuilder()
                .Append("stapler").Append("staple").AppendQuoted(path));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CleanupKeychain();
        if (_temporaryCertificateFile is not null && System.IO.File.Exists(_temporaryCertificateFile))
        {
            System.IO.File.Delete(_temporaryCertificateFile);
        }
    }

    private static string? FirstNonEmpty(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void Sign(string path, bool hardenedRuntime)
    {
        var arguments = new ProcessArgumentBuilder()
            .Append("--force")
            .Append("--timestamp")
            .Append("--sign").AppendQuoted(_identity)
            .Append("--keychain").AppendQuoted(_keychainPath);
        if (hardenedRuntime)
        {
            // The hardened runtime is required for notarization. The Native AOT binary needs
            // no JIT or unsigned memory entitlements, so none are granted.
            arguments.Append("--options").Append("runtime");
        }

        arguments.AppendQuoted(path);
        RunOrFail("codesign", arguments);
    }

    private string DetectIdentity()
    {
        RunWithOutput(
            "security",
            new ProcessArgumentBuilder()
                .Append("find-identity").Append("-v").Append("-p").Append("codesigning")
                .AppendQuoted(_keychainPath),
            out var output);

        // Lines look like: 1) <sha1> "Developer ID Application: Name (TEAMID)".
        foreach (var line in output)
        {
            var start = line.IndexOf(DeveloperIdPrefix, StringComparison.Ordinal);
            if (start < 0)
            {
                continue;
            }

            var end = line.IndexOf('"', start);
            if (end > start)
            {
                return line.Substring(start, end - start);
            }
        }

        throw new CakeException(
            "The certificate contains no Developer ID Application identity. Export a " +
            "\"Developer ID Application\" certificate (not Mac App Distribution) from the " +
            "Apple Developer account and try again.");
    }

    private string[] GetKeychainSearchList()
    {
        RunWithOutput(
            "security",
            new ProcessArgumentBuilder().Append("list-keychains").Append("-d").Append("user"),
            out var output);
        return output
            .Select(line => line.Trim().Trim('"'))
            .Where(line => line.Length > 0)
            .ToArray();
    }

    private void CleanupKeychain()
    {
        // Best effort: a cleanup failure must not mask the result of the build itself.
        try
        {
            var searchList = new ProcessArgumentBuilder()
                .Append("list-keychains").Append("-d").Append("user").Append("-s");
            foreach (var keychain in _originalKeychains)
            {
                searchList.AppendQuoted(keychain);
            }

            _context.StartProcess("security", new ProcessSettings { Arguments = searchList });
            _context.StartProcess(
                "security",
                new ProcessSettings
                {
                    Arguments = new ProcessArgumentBuilder()
                        .Append("delete-keychain").AppendQuoted(_keychainPath),
                });
        }
        catch (Exception e)
        {
            _context.Warning($"Failed to remove the temporary signing keychain {_keychainPath}: {e.Message}");
        }
    }

    private void RunOrFail(string tool, ProcessArgumentBuilder arguments)
    {
        var exitCode = _context.StartProcess(tool, new ProcessSettings { Arguments = arguments });
        if (exitCode != 0)
        {
            throw new CakeException($"{tool} failed with exit code {exitCode}.");
        }
    }

    private int RunWithOutput(string tool, ProcessArgumentBuilder arguments, out IEnumerable<string> output)
        => _context.StartProcess(
            tool,
            new ProcessSettings { Arguments = arguments, RedirectStandardOutput = true },
            out output);

    /// <summary>
    /// Credentials of Apple's notary service, read from the environment.
    /// </summary>
    /// <param name="AppleId">Apple ID e-mail address.</param>
    /// <param name="Password">App-specific password of that Apple ID.</param>
    /// <param name="TeamId">Apple Developer team identifier.</param>
    private sealed record NotaryCredentials(string AppleId, string Password, string TeamId)
    {
        /// <summary>
        /// Read the credentials, or return <c>null</c> when none are set. A partially
        /// configured set is reported and treated as absent.
        /// </summary>
        /// <param name="context">Build context used for the warning.</param>
        /// <returns>The credentials, or <c>null</c>.</returns>
        public static NotaryCredentials? TryRead(BuildContext context)
        {
            var appleId = FirstNonEmpty(Environment.GetEnvironmentVariable("CATOMANIA_MAC_NOTARY_APPLE_ID"));
            var password = FirstNonEmpty(Environment.GetEnvironmentVariable("CATOMANIA_MAC_NOTARY_PASSWORD"));
            var teamId = FirstNonEmpty(Environment.GetEnvironmentVariable("CATOMANIA_MAC_NOTARY_TEAM_ID"));
            if (appleId is null && password is null && teamId is null)
            {
                return null;
            }

            if (appleId is null || password is null || teamId is null)
            {
                context.Warning(
                    "Notarization needs all of CATOMANIA_MAC_NOTARY_APPLE_ID, CATOMANIA_MAC_NOTARY_PASSWORD " +
                    "and CATOMANIA_MAC_NOTARY_TEAM_ID, skipping it.");
                return null;
            }

            return new NotaryCredentials(appleId, password, teamId);
        }
    }
}
