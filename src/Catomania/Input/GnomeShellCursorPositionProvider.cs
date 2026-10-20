using Catomania.Simulation;
using Tmds.DBus.Protocol;

namespace Catomania.Input;

/// <summary>
/// Gets the cursor position on GNOME Wayland from a small GNOME Shell extension that shares it over D-Bus.
/// The extension is installed and enabled on the first run. GNOME Shell on Wayland discovers new extensions
/// only on login, so after the first run the user has to log out and back in once.
/// </summary>
public sealed class GnomeShellCursorPositionProvider : CompositorCursorPositionProvider
{
    /// <summary>
    /// Extension UUID, also the name of its directory.
    /// </summary>
    public const string ExtensionUuid = "catomania-cursor@catomania";

    /// <summary>
    /// Files of the extension.
    /// </summary>
    public static readonly IReadOnlyList<string> ExtensionFiles = ["metadata.json", "extension.js"];

    private const string ExtensionResourcePrefix = "Catomania.Input.Scripts.GnomeShellExtension.";
    private const string TrackerService = "org.catomania.CursorTracker";
    private const string TrackerPath = "/org/catomania/CursorTracker";
    private const string TrackerInterface = "org.catomania.CursorTracker";
    private const string ShellService = "org.gnome.Shell";
    private const string ShellPath = "/org/gnome/Shell";
    private const string ShellExtensionsInterface = "org.gnome.Shell.Extensions";
    private const long RetryMilliseconds = 5000;

    private int _isRequestInFlight;
    private long _retryTimestamp;

    /// <summary>
    /// Initializes a new instance of the <see cref="GnomeShellCursorPositionProvider" /> class.
    /// </summary>
    /// <param name="screenBoundsProvider">Provides the bounds of all screens in physical pixels.</param>
    public GnomeShellCursorPositionProvider(Func<SimulationRect>? screenBoundsProvider = null)
        : base(screenBoundsProvider)
    {
    }

    /// <inheritdoc />
    public override string Name => "GNOME Shell";

    /// <summary>
    /// Get the directory the extension is installed to.
    /// </summary>
    /// <returns>Extension directory path.</returns>
    public static string GetExtensionDirectoryPath()
    {
        var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrEmpty(dataHome))
        {
            dataHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        }

        return Path.Combine(dataHome, "gnome-shell", "extensions", ExtensionUuid);
    }

    /// <summary>
    /// Write the extension files when they are missing or outdated.
    /// </summary>
    /// <param name="directoryPath">Extension directory.</param>
    /// <returns><c>true</c> if any file has been written.</returns>
    public static bool InstallExtension(string directoryPath)
    {
        Directory.CreateDirectory(directoryPath);
        var isChanged = false;
        foreach (var fileName in ExtensionFiles)
        {
            var content = ReadExtensionFile(fileName);
            var filePath = Path.Combine(directoryPath, fileName);
            if (File.Exists(filePath) && File.ReadAllText(filePath) == content)
            {
                continue;
            }

            File.WriteAllText(filePath, content);
            isChanged = true;
        }

        return isChanged;
    }

    /// <summary>
    /// Read the extension file embedded into the application.
    /// </summary>
    /// <param name="fileName">File name.</param>
    /// <returns>File content.</returns>
    public static string ReadExtensionFile(string fileName)
    {
        var resourceName = ExtensionResourcePrefix + fileName;
        using var stream = typeof(GnomeShellCursorPositionProvider).Assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Resource {resourceName} is not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <inheritdoc />
    protected override async Task StartAsync(DBusConnection connection)
    {
        var isInstalled = InstallExtension(GetExtensionDirectoryPath());

        bool isEnabled;
        try
        {
            isEnabled = await connection.CallMethodAsync(
                    CreateMethodCall(connection, ShellService, ShellPath, ShellExtensionsInterface, "EnableExtension", ExtensionUuid),
                    (message, _) => message.GetBodyReader().ReadBool(),
                    null)
                .ConfigureAwait(false);
        }
        catch (DBusErrorReplyException e)
        {
            Console.Error.WriteLine($"Catomania: cannot enable the GNOME Shell extension {ExtensionUuid}: {e.Message}");
            isEnabled = false;
        }

        if (isInstalled || !isEnabled)
        {
            Console.Error.WriteLine(
                $"Catomania: the GNOME Shell extension {ExtensionUuid} is installed. If the cat does not follow the cursor, "
                + "log out and back in, then make sure the extension is enabled in the Extensions application.");
        }
    }

    /// <inheritdoc />
    protected override void OnPositionRequested(DBusConnection connection)
    {
        // One request at a time, the latest answer is used until the next one arrives.
        if (Environment.TickCount64 < Volatile.Read(ref _retryTimestamp)
            || Interlocked.Exchange(ref _isRequestInFlight, 1) == 1)
        {
            return;
        }

        _ = RequestPositionAsync(connection);
    }

    private async Task RequestPositionAsync(DBusConnection connection)
    {
        try
        {
            var (position, bounds) = await connection.CallMethodAsync(
                    CreateMethodCall(connection, TrackerService, TrackerPath, TrackerInterface, "GetPosition"),
                    (message, _) =>
                    {
                        var reader = message.GetBodyReader();
                        var x = reader.ReadInt32();
                        var y = reader.ReadInt32();
                        var width = reader.ReadInt32();
                        var height = reader.ReadInt32();
                        return (new ScreenPoint(x, y), new SimulationRect(0, 0, width, height));
                    },
                    null)
                .ConfigureAwait(false);
            ReportPosition(position, bounds);
        }
        catch (Exception e) when (e is DBusErrorReplyException or DBusConnectionException or ObjectDisposedException)
        {
            // The extension is not active yet: fall back to the other providers and retry later.
            ClearPosition();
            Volatile.Write(ref _retryTimestamp, Environment.TickCount64 + RetryMilliseconds);
        }
        finally
        {
            Volatile.Write(ref _isRequestInFlight, 0);
        }
    }
}
