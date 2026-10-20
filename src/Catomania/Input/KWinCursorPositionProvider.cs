using Catomania.Simulation;
using Tmds.DBus.Protocol;

namespace Catomania.Input;

/// <summary>
/// Gets the cursor position on KDE Plasma Wayland. A small KWin script is loaded over D-Bus,
/// it calls back with the position every time the cursor moves.
/// </summary>
public sealed class KWinCursorPositionProvider : CompositorCursorPositionProvider
{
    /// <summary>
    /// Object path the KWin script reports the position to.
    /// </summary>
    public const string ObjectPath = "/org/catomania/CursorTracker";

    /// <summary>
    /// Interface the KWin script reports the position to.
    /// </summary>
    public const string InterfaceName = "org.catomania.CursorTracker";

    /// <summary>
    /// Method the KWin script calls.
    /// </summary>
    public const string SetPositionMethod = "SetPosition";

    private const string ScriptResourceName = "Catomania.Input.Scripts.kwin-cursor.js";
    private const string ServicePlaceholder = "%SERVICE%";
    private const string PluginName = "catomania-cursor";
    private const string KWinService = "org.kde.KWin";
    private const string ScriptingPath = "/Scripting";
    private const string ScriptingInterface = "org.kde.kwin.Scripting";

    private string? _scriptPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="KWinCursorPositionProvider" /> class.
    /// </summary>
    /// <param name="screenBoundsProvider">Provides the bounds of all screens in physical pixels.</param>
    public KWinCursorPositionProvider(Func<SimulationRect>? screenBoundsProvider = null)
        : base(screenBoundsProvider)
    {
    }

    /// <inheritdoc />
    public override string Name => "KWin";

    /// <summary>
    /// Create the KWin script that reports the cursor position to the D-Bus service.
    /// </summary>
    /// <param name="service">D-Bus name of the application connection.</param>
    /// <returns>Script source.</returns>
    public static string CreateScript(string service)
    {
        using var stream = typeof(KWinCursorPositionProvider).Assembly.GetManifestResourceStream(ScriptResourceName)
                           ?? throw new InvalidOperationException($"Resource {ScriptResourceName} is not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace(ServicePlaceholder, service, StringComparison.Ordinal);
    }

    /// <summary>
    /// Read the <see cref="SetPositionMethod" /> arguments: the cursor position and the virtual screen geometry.
    /// KWin sends integers, doubles are accepted as well in case the script API switches to them.
    /// </summary>
    /// <param name="message">Method call message.</param>
    /// <param name="position">Cursor position in logical coordinates.</param>
    /// <param name="bounds">Virtual screen geometry in logical coordinates.</param>
    /// <returns><c>true</c> if the message has the expected signature.</returns>
    internal static bool TryReadPosition(Message message, out ScreenPoint position, out SimulationRect bounds)
    {
        position = ScreenPoint.Zero;
        bounds = SimulationRect.Empty;
        var signature = message.SignatureAsString;
        if (signature is not ("iiiiii" or "dddddd"))
        {
            return false;
        }

        var reader = message.GetBodyReader();
        var values = new double[6];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = signature[0] == 'i' ? reader.ReadInt32() : reader.ReadDouble();
        }

        position = new ScreenPoint(values[0], values[1]);
        bounds = new SimulationRect(values[2], values[3], values[4], values[5]);
        return true;
    }

    /// <inheritdoc />
    protected override async Task StartAsync(DBusConnection connection)
    {
        connection.AddMethodHandler(new PositionHandler(this));

        // KWin loads scripts from files only.
        var directory = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
        _scriptPath = Path.Combine(
            string.IsNullOrEmpty(directory) ? Path.GetTempPath() : directory,
            $"catomania-{Environment.ProcessId}-kwin-cursor.js");
        await File.WriteAllTextAsync(_scriptPath, CreateScript(connection.UniqueName!)).ConfigureAwait(false);

        // A script left by a crashed instance would prevent loading by the same name.
        await UnloadScriptAsync(connection).ConfigureAwait(false);
        var scriptId = await connection.CallMethodAsync(
                CreateMethodCall(connection, KWinService, ScriptingPath, ScriptingInterface, "loadScript", _scriptPath, PluginName),
                (message, _) => message.GetBodyReader().ReadInt32(),
                null)
            .ConfigureAwait(false);
        if (scriptId < 0)
        {
            throw new InvalidOperationException("KWin has refused to load the cursor tracking script.");
        }

        await connection.CallMethodAsync(
                CreateMethodCall(connection, KWinService, ScriptingPath, ScriptingInterface, "start"))
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task StopAsync(DBusConnection connection)
    {
        try
        {
            await UnloadScriptAsync(connection).ConfigureAwait(false);
        }
        finally
        {
            if (_scriptPath != null)
            {
                File.Delete(_scriptPath);
            }
        }
    }

    private static Task UnloadScriptAsync(DBusConnection connection)
        => connection.CallMethodAsync(
            CreateMethodCall(connection, KWinService, ScriptingPath, ScriptingInterface, "unloadScript", PluginName));

    /// <summary>
    /// Receives the position from the KWin script.
    /// </summary>
    private sealed class PositionHandler : IPathMethodHandler
    {
        private readonly KWinCursorPositionProvider _owner;

        public PositionHandler(KWinCursorPositionProvider owner)
        {
            _owner = owner;
        }

        public string Path => ObjectPath;

        public bool HandlesChildPaths => false;

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            var request = context.Request;
            if (request.InterfaceAsString != InterfaceName
                || request.MemberAsString != SetPositionMethod
                || !TryReadPosition(request, out var position, out var bounds))
            {
                context.ReplyUnknownMethodError();
                return ValueTask.CompletedTask;
            }

            _owner.ReportPosition(position, bounds);

            // KWin waits for a reply, pending calls would pile up without it.
            if (!context.NoReplyExpected)
            {
                using var writer = context.CreateReplyWriter(null!);
                context.Reply(writer.CreateMessage());
            }

            return ValueTask.CompletedTask;
        }
    }
}
