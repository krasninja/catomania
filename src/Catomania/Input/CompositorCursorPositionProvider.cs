using Catomania.Simulation;
using Tmds.DBus.Protocol;

namespace Catomania.Input;

/// <summary>
/// Base class for the providers that get the cursor position from the Wayland compositor over D-Bus.
/// Wayland does not let applications read the global cursor position, and XWayland knows it only
/// while the cursor is over an X11 window, so the compositor is the only reliable source.
/// The compositor reports logical coordinates, they are scaled to the screen pixels proportionally.
/// </summary>
public abstract class CompositorCursorPositionProvider : ICursorPositionProvider, IDisposable
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(1);

    private readonly Func<SimulationRect> _screenBoundsProvider;
    private readonly Lock _syncRoot = new();

    private DBusConnection? _connection;
    private Task? _startTask;
    private ScreenPoint _logicalPosition;
    private SimulationRect _logicalBounds;
    private bool _hasPosition;
    private volatile bool _failed;
    private volatile bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositorCursorPositionProvider" /> class.
    /// </summary>
    /// <param name="screenBoundsProvider">Provides the bounds of all screens in physical pixels, used to scale the logical coordinates.</param>
    protected CompositorCursorPositionProvider(Func<SimulationRect>? screenBoundsProvider)
    {
        _screenBoundsProvider = screenBoundsProvider ?? (() => SimulationRect.Empty);
    }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public bool IsAvailable => !_failed && !_disposed;

    /// <summary>
    /// Scale the logical compositor coordinates to the screen pixels.
    /// </summary>
    /// <param name="logicalPosition">Cursor position in logical coordinates.</param>
    /// <param name="logicalBounds">Bounds of all screens in logical coordinates.</param>
    /// <param name="screenBounds">Bounds of all screens in physical pixels.</param>
    /// <returns>Cursor position in physical pixels.</returns>
    public static ScreenPoint MapToScreen(ScreenPoint logicalPosition, SimulationRect logicalBounds, SimulationRect screenBounds)
    {
        if (logicalBounds.IsEmpty || screenBounds.IsEmpty)
        {
            return logicalPosition;
        }

        return new ScreenPoint(
            screenBounds.X + ((logicalPosition.X - logicalBounds.X) * screenBounds.Width / logicalBounds.Width),
            screenBounds.Y + ((logicalPosition.Y - logicalBounds.Y) * screenBounds.Height / logicalBounds.Height));
    }

    /// <inheritdoc />
    public bool TryGetPosition(out ScreenPoint position)
    {
        position = ScreenPoint.Zero;
        if (!IsAvailable)
        {
            return false;
        }

        // The connection is set up in the background, other providers are used until the first position arrives.
        _startTask ??= Task.Run(StartAsync);
        if (_connection is { } connection && _startTask.IsCompletedSuccessfully && !_failed)
        {
            OnPositionRequested(connection);
        }

        ScreenPoint logicalPosition;
        SimulationRect logicalBounds;
        lock (_syncRoot)
        {
            if (!_hasPosition)
            {
                return false;
            }

            logicalPosition = _logicalPosition;
            logicalBounds = _logicalBounds;
        }

        position = MapToScreen(logicalPosition, logicalBounds, _screenBoundsProvider());
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Undo the compositor side changes and close the connection.
    /// </summary>
    /// <param name="disposing"><c>true</c> when called from <see cref="Dispose()" />.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
        {
            return;
        }

        _disposed = true;
        var connection = _connection;
        if (connection == null)
        {
            return;
        }

        try
        {
            // Run on the thread pool, so the D-Bus continuations do not need the UI thread that waits here.
            if (_startTask is { IsCompletedSuccessfully: true } && !_failed)
            {
                Task.Run(() => StopAsync(connection)).Wait(StopTimeout);
            }
        }
        catch (AggregateException e)
        {
            Console.Error.WriteLine($"Catomania: {Name} cursor tracking cleanup failed: {e.InnerException?.Message}");
        }
        finally
        {
            connection.Dispose();
        }
    }

    /// <summary>
    /// Set up the compositor side: load a script, install an extension and so on.
    /// </summary>
    /// <param name="connection">Connected session bus.</param>
    /// <returns>Task.</returns>
    protected abstract Task StartAsync(DBusConnection connection);

    /// <summary>
    /// Undo what <see cref="StartAsync(DBusConnection)" /> did. Called on dispose.
    /// </summary>
    /// <param name="connection">Connected session bus.</param>
    /// <returns>Task.</returns>
    protected virtual Task StopAsync(DBusConnection connection) => Task.CompletedTask;

    /// <summary>
    /// Called every time the position is read, the providers that poll the compositor request a new position here.
    /// It must not block.
    /// </summary>
    /// <param name="connection">Connected session bus.</param>
    protected virtual void OnPositionRequested(DBusConnection connection)
    {
    }

    /// <summary>
    /// Remember the position reported by the compositor. Can be called from any thread.
    /// </summary>
    /// <param name="logicalPosition">Cursor position in logical coordinates.</param>
    /// <param name="logicalBounds">Bounds of all screens in logical coordinates.</param>
    protected void ReportPosition(ScreenPoint logicalPosition, SimulationRect logicalBounds)
    {
        lock (_syncRoot)
        {
            _logicalPosition = logicalPosition;
            _logicalBounds = logicalBounds;
            _hasPosition = true;
        }
    }

    /// <summary>
    /// Forget the last position, so the other providers are used until a new one is reported.
    /// </summary>
    protected void ClearPosition()
    {
        lock (_syncRoot)
        {
            _hasPosition = false;
        }
    }

    /// <summary>
    /// Create a method call message.
    /// </summary>
    /// <param name="connection">Session bus.</param>
    /// <param name="destination">Destination service.</param>
    /// <param name="path">Object path.</param>
    /// <param name="interfaceName">Interface name.</param>
    /// <param name="member">Method name.</param>
    /// <param name="arguments">String arguments.</param>
    /// <returns>Message.</returns>
    protected static MessageBuffer CreateMethodCall(
        DBusConnection connection,
        string destination,
        string path,
        string interfaceName,
        string member,
        params string[] arguments)
    {
        using var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination,
            path,
            interfaceName,
            member,
            arguments.Length > 0 ? new string('s', arguments.Length) : null,
            MessageFlags.None);
        foreach (var argument in arguments)
        {
            writer.WriteString(argument);
        }

        return writer.CreateMessage();
    }

    private async Task StartAsync()
    {
        try
        {
            var address = DBusAddress.Session
                          ?? throw new InvalidOperationException("The session bus address is not set.");
            var connection = new DBusConnection(address);
            await connection.ConnectAsync().ConfigureAwait(false);
            _connection = connection;
            if (_disposed)
            {
                return;
            }

            await StartAsync(connection).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            // The other providers keep working, the animal just does not see the cursor far away.
            _failed = true;
            Console.Error.WriteLine($"Catomania: {Name} cursor tracking is not available: {e.Message}");
        }
    }
}
