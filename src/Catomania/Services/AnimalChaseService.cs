using Avalonia.Threading;
using Catomania.Input;
using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Services;

/// <summary>
/// Runs the chase loop: reads the cursor position, advances <see cref="AnimalSimulator" /> and moves the window.
/// </summary>
public sealed class AnimalChaseService : IDisposable
{
    /// <summary>
    /// Interval between simulation ticks, roughly 60 frames per second.
    /// </summary>
    public static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(16);

    private const double WorkAreasRefreshSeconds = 1;
    private const double SpawnOffsetY = 90;

    private readonly IAnimalHost _host;
    private readonly ICursorPositionProvider _cursorPositionProvider;
    private readonly AnimalSimulator _simulator;
    private readonly AppSettings _settings;

    private DispatcherTimer? _timer;

    private IReadOnlyList<SimulationRect> _workAreas = [];
    private SimulationRect _bounds;
    private ScreenPoint _lastCursorPosition;
    private bool _hasCursorPosition;
    private long _lastTickTimestamp;
    private double _workAreasRefreshTime = WorkAreasRefreshSeconds;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimalChaseService" /> class.
    /// </summary>
    /// <param name="host">Window the animal lives in.</param>
    /// <param name="cursorPositionProvider">Cursor position provider.</param>
    /// <param name="simulator">Animal simulator.</param>
    /// <param name="settings">Application settings.</param>
    public AnimalChaseService(
        IAnimalHost host,
        ICursorPositionProvider cursorPositionProvider,
        AnimalSimulator simulator,
        AppSettings settings)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _cursorPositionProvider = cursorPositionProvider ?? throw new ArgumentNullException(nameof(cursorPositionProvider));
        _simulator = simulator ?? throw new ArgumentNullException(nameof(simulator));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ApplySettings();
    }

    /// <summary>
    /// Gets the animal simulator.
    /// </summary>
    public AnimalSimulator Simulator => _simulator;

    /// <summary>
    /// Gets the area the animal is allowed to walk in.
    /// </summary>
    public SimulationRect Bounds => _bounds;

    /// <summary>
    /// Start the loop.
    /// </summary>
    public void Start()
    {
        _workAreas = _host.GetWorkAreas();
        var center = _host.GetCenter();
        _bounds = ComputeBounds(center);
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
    /// Put the animal next to the cursor. When the cursor position is not available the animal stays
    /// where the window is.
    /// </summary>
    public void Recenter()
    {
        TryGetCursorPosition(out var cursorPosition);
        var position = _hasCursorPosition
            ? new ScreenPoint(cursorPosition.X, cursorPosition.Y + SpawnOffsetY)
            : _host.GetCenter();

        // Recenter is called before the loop starts, when the work areas are not read yet.
        _workAreas = _host.GetWorkAreas();
        _bounds = ComputeBounds(cursorPosition);
        _simulator.Teleport(_bounds.Clamp(position));
    }

    /// <summary>
    /// Advance the simulation and move the window.
    /// </summary>
    /// <param name="deltaSeconds">Time elapsed since the previous tick in seconds.</param>
    public void Tick(double deltaSeconds)
    {
        RefreshWorkAreas(deltaSeconds);
        TryGetCursorPosition(out var cursorPosition);

        if (_host.IsDragging)
        {
            _simulator.Teleport(_host.GetCenter());
        }

        // Computed every tick, so the bounds follow the animal dragged to another screen and the sprite size changes at once.
        _bounds = ComputeBounds(_simulator.Position);

        _simulator.Update(deltaSeconds, new AnimalInput(
            cursorPosition,
            _bounds,
            _settings.Speed.ToMultiplier(),
            _host.IsDragging,
            _settings.Paused,
            Math.Min(_host.SpriteSize.X, _host.SpriteSize.Y)));

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

        // Keep the last known position, or stay where the animal is when it has never been read.
        cursorPosition = _hasCursorPosition ? _lastCursorPosition : _host.GetCenter();
        return false;
    }

    private void RefreshWorkAreas(double deltaSeconds)
    {
        _workAreasRefreshTime += deltaSeconds;
        if (_workAreasRefreshTime < WorkAreasRefreshSeconds && _workAreas.Count > 0)
        {
            return;
        }

        _workAreasRefreshTime = 0;
        _workAreas = _host.GetWorkAreas();
    }

    private SimulationRect ComputeBounds(ScreenPoint position)
    {
        if (_workAreas.Count == 0)
        {
            return SimulationRect.Empty;
        }

        // The animal stays on the screen it is on and does not follow the cursor to another screen,
        // it can be moved there by dragging or recentering. Fall back to all screens combined.
        var area = FindWorkArea(position);
        if (area.IsEmpty)
        {
            foreach (var workArea in _workAreas)
            {
                area = SimulationRect.Union(area, workArea);
            }
        }

        // Keep the whole animal inside the area.
        var margin = Math.Max(_host.SpriteSize.X, _host.SpriteSize.Y) / 2;
        return area.Deflate(margin);
    }

    private SimulationRect FindWorkArea(ScreenPoint point)
    {
        foreach (var workArea in _workAreas)
        {
            if (workArea.Contains(point))
            {
                return workArea;
            }
        }

        return SimulationRect.Empty;
    }
}
