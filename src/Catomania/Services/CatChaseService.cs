using Avalonia.Threading;
using Catomania.Input;
using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Services;

/// <summary>
/// Runs the chase loop: reads the cursor position, advances <see cref="CatSimulator" /> and moves the window.
/// </summary>
public sealed class CatChaseService : IDisposable
{
    /// <summary>
    /// Interval between simulation ticks, roughly 60 frames per second.
    /// </summary>
    public static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(16);

    private const double BoundsRefreshSeconds = 1;
    private const double SpawnOffsetY = 90;

    private readonly ICatHost _host;
    private readonly ICursorPositionProvider _cursorPositionProvider;
    private readonly CatSimulator _simulator;
    private readonly AppSettings _settings;

    private DispatcherTimer? _timer;

    private IReadOnlyList<SimulationRect> _workAreas = [];
    private SimulationRect _bounds;
    private ScreenPoint _lastCursorPosition;
    private bool _hasCursorPosition;
    private long _lastTickTimestamp;
    private double _boundsRefreshTime = BoundsRefreshSeconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatChaseService" /> class.
    /// </summary>
    /// <param name="host">Window the cat lives in.</param>
    /// <param name="cursorPositionProvider">Cursor position provider.</param>
    /// <param name="simulator">Cat simulator.</param>
    /// <param name="settings">Application settings.</param>
    public CatChaseService(
        ICatHost host,
        ICursorPositionProvider cursorPositionProvider,
        CatSimulator simulator,
        AppSettings settings)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _cursorPositionProvider = cursorPositionProvider ?? throw new ArgumentNullException(nameof(cursorPositionProvider));
        _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ApplySettings();
    }

    /// <summary>
    /// Gets the cat simulator.
    /// </summary>
    public CatSimulator Simulator => _simulator;

    /// <summary>
    /// Gets the area the cat is allowed to walk in.
    /// </summary>
    public SimulationRect Bounds => _bounds;

    /// <summary>
    /// Start the loop.
    /// </summary>
    public void Start()
    {
        _workAreas = _host.GetWorkAreas();
        _bounds = ComputeBounds(_host.GetCenter());
        _lastTickTimestamp = Environment.TickCount64;

        // The timer is created lazily, so the service can be used without a running dispatcher.
        if (_timer == null)
        {
            _timer = new DispatcherTimer { Interval = TickInterval };
            _timer.Tick += OnTimerTick;
        }

        _timer.Start();
    }

    /// <summary>
    /// Stop the loop.
    /// </summary>
    public void Stop() => _timer?.Stop();

    /// <summary>
    /// Apply the settings that affect the simulation.
    /// </summary>
    public void ApplySettings() => _simulator.Options.SleepAfterSeconds = Math.Max(1, _settings.SleepAfterSeconds);

    /// <summary>
    /// Put the cat next to the cursor. When the cursor position is not available the cat stays
    /// where the window is.
    /// </summary>
    public void Recenter()
    {
        TryGetCursorPosition(out var cursorPosition);
        var position = _hasCursorPosition
            ? new ScreenPoint(cursorPosition.X, cursorPosition.Y + SpawnOffsetY)
            : _host.GetCenter();

        _bounds = ComputeBounds(cursorPosition);
        _simulator.Teleport(_bounds.Clamp(position));
    }

    /// <summary>
    /// Advance the simulation and move the window.
    /// </summary>
    /// <param name="deltaSeconds">Time elapsed since the previous tick in seconds.</param>
    public void Tick(double deltaSeconds)
    {
        RefreshBounds(deltaSeconds);
        TryGetCursorPosition(out var cursorPosition);

        if (_host.IsDragging)
        {
            _simulator.Teleport(_host.GetCenter());
        }

        _simulator.Update(deltaSeconds, new CatInput(
            cursorPosition,
            _bounds,
            _settings.Speed.ToMultiplier(),
            _host.IsDragging,
            _settings.Paused));

        if (!_host.IsDragging)
        {
            _host.SetCenter(_simulator.Position);
        }

        _host.ApplyRenderState(_simulator.CreateRenderState());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        (_cursorPositionProvider as IDisposable)?.Dispose();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        var timestamp = Environment.TickCount64;
        var deltaSeconds = (timestamp - _lastTickTimestamp) / 1000.0;
        _lastTickTimestamp = timestamp;
        Tick(deltaSeconds);
    }

    private bool TryGetCursorPosition(out ScreenPoint cursorPosition)
    {
        if (_cursorPositionProvider.TryGetPosition(out var position))
        {
            _lastCursorPosition = position;
            _hasCursorPosition = true;
            cursorPosition = position;
            return true;
        }

        // Keep the last known position, or stay where the cat is when it has never been read.
        cursorPosition = _hasCursorPosition ? _lastCursorPosition : _host.GetCenter();
        return false;
    }

    private void RefreshBounds(double deltaSeconds)
    {
        _boundsRefreshTime += deltaSeconds;
        if (_boundsRefreshTime < BoundsRefreshSeconds && !_bounds.IsEmpty)
        {
            return;
        }

        _boundsRefreshTime = 0;
        _workAreas = _host.GetWorkAreas();
        _bounds = ComputeBounds(_hasCursorPosition ? _lastCursorPosition : _host.GetCenter());
    }

    private SimulationRect ComputeBounds(ScreenPoint cursorPosition)
    {
        if (_workAreas.Count == 0)
        {
            return SimulationRect.Empty;
        }

        // Prefer the screen the cursor is on, fall back to all screens combined.
        var area = SimulationRect.Empty;
        foreach (var workArea in _workAreas)
        {
            if (workArea.Contains(cursorPosition))
            {
                area = workArea;
                break;
            }
        }

        if (area.IsEmpty)
        {
            foreach (var workArea in _workAreas)
            {
                area = SimulationRect.Union(area, workArea);
            }
        }

        // Keep the whole cat inside the area.
        var margin = Math.Max(_host.SpriteSize.X, _host.SpriteSize.Y) / 2;
        return area.Deflate(margin);
    }
}
