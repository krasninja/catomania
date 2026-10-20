using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Catomania.Controls;
using Catomania.Input;
using Catomania.Models;
using Catomania.Services;
using Catomania.Simulation;

namespace Catomania.Views;

/// <summary>
/// A small transparent window the cat lives in. It is moved by <see cref="CatChaseService" />
/// and can be dragged around with the mouse.
/// </summary>
public sealed partial class CatWindow : Window, ICatHost, IDisposable
{
    private readonly AppSettings _settings;
    private readonly CatView _catView;
    private readonly ContextMenu _contextMenu;
    private readonly MenuItem _pauseMenuItem;
    private readonly MenuItem _topmostMenuItem;
    private readonly FallbackCursorPositionProvider _fallbackCursorPositionProvider = new();
    private readonly CompositeCursorPositionProvider _cursorPositionProvider;
    private readonly CatSimulator _simulator = new();
    private readonly CatChaseService _chaseService;

    private bool _isDragging;
    private bool _isCheckingUpdates;
    private bool _disposed;
    private PixelPoint _dragWindowPosition;
    private PixelPoint _dragPointerPosition;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatWindow" /> class with the default settings.
    /// Used by the XAML designer.
    /// </summary>
    public CatWindow()
        : this(new AppSettings())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CatWindow" /> class.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    public CatWindow(AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        InitializeComponent();

        _catView = this.GetControl<CatView>("Cat");
        _contextMenu = this.GetControl<ContextMenu>("CatContextMenu");
        _pauseMenuItem = this.GetControl<MenuItem>("PauseMenuItem");
        _topmostMenuItem = this.GetControl<MenuItem>("TopmostMenuItem");

        _cursorPositionProvider = CursorPositionProviderFactory.CreateDefault(
            _fallbackCursorPositionProvider,
            () => Scaling);
        _chaseService = new CatChaseService(this, _cursorPositionProvider, _simulator, _settings);

        ApplySettings();
        SyncMenu();

        _contextMenu.Opening += (_, _) => SyncMenu();
        Opened += OnOpened;
        Closed += OnClosed;
    }

    /// <summary>
    /// Occurs when the user changes the settings.
    /// </summary>
    public event EventHandler? SettingsChanged;

    /// <summary>
    /// Gets the screen scaling of the window.
    /// </summary>
    public double Scaling => RenderScaling > 0 ? RenderScaling : 1;

    /// <inheritdoc />
    double ICatHost.Scaling => Scaling;

    /// <inheritdoc />
    bool ICatHost.IsDragging => _isDragging;

    /// <inheritdoc />
    ScreenPoint ICatHost.SpriteSize
    {
        get
        {
            var size = GetSpriteLogicalSize();
            return new ScreenPoint(size.Width * Scaling, size.Height * Scaling);
        }
    }

    /// <inheritdoc />
    ScreenPoint ICatHost.GetCenter() => GetCenter();

    /// <inheritdoc />
    void ICatHost.SetCenter(ScreenPoint center) => SetCenter(center);

    /// <inheritdoc />
    IReadOnlyList<SimulationRect> ICatHost.GetWorkAreas()
    {
        var workAreas = new List<SimulationRect>();
        var screens = Screens?.All;
        if (screens == null)
        {
            return workAreas;
        }

        foreach (var screen in screens)
        {
            var workingArea = screen.WorkingArea;
            workAreas.Add(new SimulationRect(workingArea.X, workingArea.Y, workingArea.Width, workingArea.Height));
        }

        return workAreas;
    }

    /// <inheritdoc />
    void ICatHost.ApplyRenderState(CatRenderState state) => _catView.Update(state);

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        ReportCursorPosition(e);
        if (e.ClickCount == 2)
        {
            _settings.Paused = !_settings.Paused;
            CommitSettings();
            return;
        }

        _isDragging = true;
        _dragWindowPosition = Position;
        _dragPointerPosition = this.PointToScreen(e.GetPosition(this));
        e.Pointer.Capture(this);
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        ReportCursorPosition(e);
        if (!_isDragging)
        {
            return;
        }

        var pointerPosition = this.PointToScreen(e.GetPosition(this));
        Position = new PixelPoint(
            _dragWindowPosition.X + (pointerPosition.X - _dragPointerPosition.X),
            _dragWindowPosition.Y + (pointerPosition.Y - _dragPointerPosition.Y));
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_isDragging)
        {
            return;
        }

        e.Pointer.Capture(null);
        EndDrag();
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        // The capture can be taken away without a release, for example by a window switch,
        // the cat would stay dragged forever otherwise.
        EndDrag();
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Delta.Y == 0)
        {
            return;
        }

        var sizes = Enum.GetValues<CatSize>();
        var index = Array.IndexOf(sizes, _settings.Size);
        var size = sizes[Math.Clamp(index + (e.Delta.Y > 0 ? 1 : -1), 0, sizes.Length - 1)];
        if (size == _settings.Size)
        {
            return;
        }

        _settings.Size = size;
        CommitSettings();
    }

    private void EndDrag()
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        _simulator.Teleport(GetCenter());
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        _chaseService.Recenter();
        SetCenter(_simulator.Position);
        _chaseService.Start();
    }

    private void OnClosed(object? sender, EventArgs e) => DisposeResources();

    private void DisposeResources()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _chaseService.Dispose();
        _cursorPositionProvider.Dispose();
    }

    private void OnPauseClick(object? sender, RoutedEventArgs e)
    {
        _settings.Paused = !_settings.Paused;
        CommitSettings();
    }

    private void OnSpeedClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: CatSpeed speed })
        {
            _settings.Speed = speed;
        }

        CommitSettings();
    }

    private void OnColorSchemeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: CatColorScheme colorScheme })
        {
            _settings.ColorScheme = colorScheme;
        }

        CommitSettings();
    }

    private void OnSizeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: CatSize size })
        {
            _settings.Size = size;
        }

        CommitSettings();
    }

    private void OnTopmostClick(object? sender, RoutedEventArgs e)
    {
        _settings.AlwaysOnTop = !_settings.AlwaysOnTop;
        CommitSettings();
    }

    private void OnRecenterClick(object? sender, RoutedEventArgs e) => _chaseService.Recenter();

    private async void OnCheckUpdatesClick(object? sender, RoutedEventArgs e)
    {
        // An async void handler must not throw, and a second click should not start a parallel update.
        if (_isCheckingUpdates)
        {
            return;
        }

        _isCheckingUpdates = true;
        try
        {
            var report = await new AppUpdateService().CheckAndUpdateAsync(restartAfterUpdate: true);
            if (report.Kind != UpdateReportKind.Updating)
            {
                ShowUpdateReport(report.Message);
            }
        }
        catch (Exception exception)
        {
            ShowUpdateReport($"Failed to check updates: {exception.Message}");
        }
        finally
        {
            _isCheckingUpdates = false;
        }
    }

    private void ShowUpdateReport(string message)
    {
        var dialog = new Window
        {
            Title = "Catomania Updates",
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            ShowInTaskbar = false,
            Content = new TextBlock
            {
                Text = message,
                Margin = new Thickness(16),
                MaxWidth = 360,
                TextWrapping = TextWrapping.Wrap,
            },
        };
        _ = dialog.ShowDialog(this);
    }

    private void OnQuitClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
        else
        {
            Close();
        }
    }

    private void ApplySettings()
    {
        var size = _settings.Size.ToLogicalSize();
        Width = size;
        Height = size;
        Topmost = _settings.AlwaysOnTop;
        _catView.ColorScheme = _settings.ColorScheme;
        _chaseService.ApplySettings();
    }

    private void CommitSettings()
    {
        ApplySettings();
        SyncMenu();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SyncMenu()
    {
        _pauseMenuItem.IsChecked = _settings.Paused;
        _topmostMenuItem.IsChecked = _settings.AlwaysOnTop;
        foreach (var item in GetMenuItems())
        {
            item.IsChecked = item.Tag switch
            {
                CatSpeed speed => speed == _settings.Speed,
                CatColorScheme colorScheme => colorScheme == _settings.ColorScheme,
                CatSize size => size == _settings.Size,
                _ => item.IsChecked,
            };
        }
    }

    private IEnumerable<MenuItem> GetMenuItems()
    {
        foreach (var item in _contextMenu.Items.OfType<MenuItem>())
        {
            yield return item;
            foreach (var child in item.Items.OfType<MenuItem>())
            {
                yield return child;
            }
        }
    }

    private Size GetSpriteLogicalSize()
    {
        var size = ClientSize;
        if (size.Width > 0 && size.Height > 0)
        {
            return size;
        }

        return new Size(
            double.IsNaN(Width) ? _settings.Size.ToLogicalSize() : Width,
            double.IsNaN(Height) ? _settings.Size.ToLogicalSize() : Height);
    }

    private ScreenPoint GetCenter()
    {
        var size = ((ICatHost)this).SpriteSize;
        return new ScreenPoint(Position.X + (size.X / 2), Position.Y + (size.Y / 2));
    }

    private void SetCenter(ScreenPoint center)
    {
        var size = ((ICatHost)this).SpriteSize;
        Position = new PixelPoint(
            (int)Math.Round(center.X - (size.X / 2)),
            (int)Math.Round(center.Y - (size.Y / 2)));
    }

    private void ReportCursorPosition(PointerEventArgs e)
    {
        var screenPosition = this.PointToScreen(e.GetPosition(this));
        _fallbackCursorPositionProvider.Report(new ScreenPoint(screenPosition.X, screenPosition.Y));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeResources();
        GC.SuppressFinalize(this);
    }
}
