using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Catomania.Animals;
using Catomania.Controls;
using Catomania.Input;
using Catomania.Models;
using Catomania.Services;
using Catomania.Simulation;

namespace Catomania.Views;

/// <summary>
/// A small transparent window the animal lives in. It is moved by <see cref="AnimalChaseService" />
/// and can be dragged around with the mouse.
/// </summary>
public sealed partial class AnimalWindow : Window, IAnimalHost, IAnimalCommands, IDisposable
{
    /// <summary>
    /// Delay before the first automatic update check, so it does not slow down the start.
    /// </summary>
    public static readonly TimeSpan FirstUpdateCheckDelay = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Interval between the automatic update checks.
    /// </summary>
    public static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(6);

    private readonly AppSettings _settings;
    private readonly AnimalView _animalView;
    private readonly ContextMenu _contextMenu;
    private readonly MenuItem _pauseMenuItem;
    private readonly MenuItem _furMenuItem;
    private readonly MenuItem _topmostMenuItem;
    private readonly FallbackCursorPositionProvider _fallbackCursorPositionProvider = new();
    private readonly CompositeCursorPositionProvider _cursorPositionProvider;
    private readonly AnimalSimulator _simulator;
    private readonly AnimalChaseService _chaseService;
    private readonly DispatcherTimer _updateCheckTimer;

    private bool _isDragging;
    private bool _isCheckingUpdates;
    private bool _disposed;
    private PixelPoint _dragWindowPosition;
    private PixelPoint _dragPointerPosition;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimalWindow" /> class with the default settings.
    /// Used by the XAML designer.
    /// </summary>
    public AnimalWindow()
        : this(new AppSettings())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimalWindow" /> class.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    public AnimalWindow(AppSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        InitializeComponent();

        _animalView = this.GetControl<AnimalView>("AnimalView");
        _contextMenu = this.GetControl<ContextMenu>("AnimalContextMenu");
        _pauseMenuItem = this.GetControl<MenuItem>("PauseMenuItem");
        _furMenuItem = this.GetControl<MenuItem>("FurMenuItem");
        _topmostMenuItem = this.GetControl<MenuItem>("TopmostMenuItem");

        _cursorPositionProvider = CursorPositionProviderFactory.CreateDefault(
            _fallbackCursorPositionProvider,
            () => Scaling,
            GetScreenBounds);
        _animalView.Animal = Animal.Create(_settings);
        _simulator = new AnimalSimulator(_animalView.Animal.CreateSimulatorOptions());
        _chaseService = new AnimalChaseService(this, _cursorPositionProvider, _simulator, _settings);

        _updateCheckTimer = new DispatcherTimer { Interval = FirstUpdateCheckDelay };
        _updateCheckTimer.Tick += OnUpdateCheckTimerTick;

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

    /// <inheritdoc />
    public void TogglePause()
    {
        _settings.Paused = !_settings.Paused;
        CommitSettings();
    }

    /// <inheritdoc />
    public void SetAnimal(AnimalKind kind)
    {
        _settings.Animal = kind;
        CommitSettings();
    }

    /// <inheritdoc />
    public void SetSpeed(AnimalSpeed speed)
    {
        _settings.Speed = speed;
        CommitSettings();
    }

    /// <inheritdoc />
    public void SetFur(CatColorScheme colorScheme)
    {
        _settings.ColorScheme = colorScheme;
        CommitSettings();
    }

    /// <inheritdoc />
    public void SetSize(AnimalSize size)
    {
        _settings.Size = size;
        CommitSettings();
    }

    /// <inheritdoc />
    public void ToggleAlwaysOnTop()
    {
        _settings.AlwaysOnTop = !_settings.AlwaysOnTop;
        CommitSettings();
    }

    /// <inheritdoc />
    public void Recenter() => _chaseService.Recenter();

    /// <inheritdoc />
    public Task CheckForUpdatesAsync() => CheckForUpdatesAsync(silent: false);

    /// <inheritdoc />
    public void Quit()
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

    /// <summary>
    /// Gets the screen scaling of the window.
    /// </summary>
    public double Scaling => RenderScaling > 0 ? RenderScaling : 1;

    /// <inheritdoc />
    double IAnimalHost.Scaling => Scaling;

    /// <inheritdoc />
    bool IAnimalHost.IsDragging => _isDragging;

    /// <inheritdoc />
    ScreenPoint IAnimalHost.SpriteSize
    {
        get
        {
            var size = GetSpriteLogicalSize();
            return new ScreenPoint(size.Width * Scaling, size.Height * Scaling);
        }
    }

    /// <inheritdoc />
    ScreenPoint IAnimalHost.GetCenter() => GetCenter();

    /// <inheritdoc />
    void IAnimalHost.SetCenter(ScreenPoint center) => SetCenter(center);

    /// <inheritdoc />
    IReadOnlyList<SimulationRect> IAnimalHost.GetWorkAreas()
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
    void IAnimalHost.ApplyRenderState(AnimalRenderState state) => _animalView.Update(state);

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
        // the animal would stay dragged forever otherwise.
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

        var sizes = Enum.GetValues<AnimalSize>();
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
        _updateCheckTimer.Start();
    }

    private void OnClosed(object? sender, EventArgs e) => DisposeResources();

    private void DisposeResources()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _updateCheckTimer.Stop();
        _chaseService.Dispose();
        _cursorPositionProvider.Dispose();
    }

    private void OnPauseClick(object? sender, RoutedEventArgs e) => TogglePause();

    private void OnAnimalClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: AnimalKind kind })
        {
            SetAnimal(kind);
        }
    }

    private void OnSpeedClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: AnimalSpeed speed })
        {
            SetSpeed(speed);
        }
    }

    private void OnColorSchemeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: CatColorScheme colorScheme })
        {
            SetFur(colorScheme);
        }
    }

    private void OnSizeClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: AnimalSize size })
        {
            SetSize(size);
        }
    }

    private void OnTopmostClick(object? sender, RoutedEventArgs e) => ToggleAlwaysOnTop();

    private void OnRecenterClick(object? sender, RoutedEventArgs e) => Recenter();

    private async void OnCheckUpdatesClick(object? sender, RoutedEventArgs e) => await CheckForUpdatesAsync();

    private async void OnUpdateCheckTimerTick(object? sender, EventArgs e)
    {
        _updateCheckTimer.Interval = UpdateCheckInterval;
        await CheckForUpdatesAsync(silent: true);
    }

    /// <summary>
    /// Check the release feed and apply a newer version, restarting the application.
    /// </summary>
    /// <param name="silent">
    /// Whether the check runs in the background: it is skipped for the builds that cannot update
    /// themselves, and no report is shown, failures are only logged.
    /// </param>
    private async Task CheckForUpdatesAsync(bool silent)
    {
        // A second call should not start a parallel update.
        if (_isCheckingUpdates)
        {
            return;
        }

        _isCheckingUpdates = true;
        try
        {
            var updateService = new AppUpdateService();
            if (silent && !updateService.IsInstalled)
            {
                return;
            }

            var report = await updateService.CheckAndUpdateAsync(restartAfterUpdate: true);
            if (silent)
            {
                if (report.Kind == UpdateReportKind.Error)
                {
                    Console.Error.WriteLine($"Catomania: {report.Message}");
                }
            }
            else if (report.Kind != UpdateReportKind.Updating)
            {
                ShowUpdateReport(report.Message);
            }
        }
        catch (Exception exception)
        {
            if (silent)
            {
                Console.Error.WriteLine($"Catomania: failed to check updates: {exception.Message}");
            }
            else
            {
                ShowUpdateReport($"Failed to check updates: {exception.Message}");
            }
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

    private void OnQuitClick(object? sender, RoutedEventArgs e) => Quit();

    private void ApplySettings()
    {
        var size = _settings.Size.ToLogicalSize();
        Width = size;
        Height = size;
        Topmost = _settings.AlwaysOnTop;
        _animalView.Animal = Animal.Create(_settings);

        // Animals take strides of different length, the gait would slide after a switch otherwise.
        var options = _animalView.Animal.CreateSimulatorOptions();
        _simulator.Options.WalkStrideLength = options.WalkStrideLength;
        _simulator.Options.RunStrideLength = options.RunStrideLength;
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
        _furMenuItem.IsEnabled = _settings.Animal == AnimalKind.Cat;
        foreach (var item in GetMenuItems())
        {
            item.IsChecked = item.Tag switch
            {
                AnimalKind kind => kind == _settings.Animal,
                AnimalSpeed speed => speed == _settings.Speed,
                CatColorScheme colorScheme => colorScheme == _settings.ColorScheme,
                AnimalSize size => size == _settings.Size,
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

    private SimulationRect GetScreenBounds()
    {
        // Called by the cursor providers from the UI thread, as part of the chase loop tick.
        var bounds = SimulationRect.Empty;
        foreach (var screen in Screens?.All ?? [])
        {
            var screenBounds = screen.Bounds;
            bounds = SimulationRect.Union(
                bounds,
                new SimulationRect(screenBounds.X, screenBounds.Y, screenBounds.Width, screenBounds.Height));
        }

        return bounds;
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
        var size = ((IAnimalHost)this).SpriteSize;
        return new ScreenPoint(Position.X + (size.X / 2), Position.Y + (size.Y / 2));
    }

    private void SetCenter(ScreenPoint center)
    {
        var size = ((IAnimalHost)this).SpriteSize;
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
