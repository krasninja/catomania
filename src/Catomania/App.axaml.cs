using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Catomania.Abstractions;
using Catomania.Models;
using Catomania.Services;
using Catomania.Views;

namespace Catomania;

/// <summary>
/// Avalonia <see cref="Application" /> class.
/// </summary>
public sealed partial class App : Application
{
    /// <summary>
    /// Tray menu item parameter of the pause check box.
    /// </summary>
    public const string PauseSetting = nameof(AppSettings.Paused);

    /// <summary>
    /// Tray menu item parameter of the fur submenu, which is enabled only for the cat.
    /// </summary>
    public const string FurSetting = nameof(AppSettings.ColorScheme);

    /// <summary>
    /// Tray menu item parameter of the always on top check box.
    /// </summary>
    public const string AlwaysOnTopSetting = nameof(AppSettings.AlwaysOnTop);

    /// <summary>
    /// Tray menu item parameter of the launch at system startup check box.
    /// </summary>
    public const string LaunchAtStartupSetting = nameof(LaunchAtStartupSetting);

    private readonly SettingsStorage _settingsStorage = new();

    private AppSettings? _settings;
    private IAnimalCommands? _commands;
    private IAppCommands? _appCommands;
    private ISystemAutostart? _systemAutostart;
    private PosixSignalRegistration? _terminationRegistration;

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Connect the tray menu declared in the application XAML to the animal and show the tray icon.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    /// <param name="commands">The animal commands to run when the menu items are clicked.</param>
    /// <param name="appCommands">The application commands to run when the menu items are clicked.</param>
    public void AttachTrayIcon(AppSettings settings, IAnimalCommands commands, IAppCommands appCommands)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        _appCommands = appCommands ?? throw new ArgumentNullException(nameof(appCommands));
        SyncTrayMenu();
        foreach (var trayIcon in TrayIcon.GetIcons(this) ?? [])
        {
            trayIcon.IsVisible = true;
        }
    }

    /// <summary>
    /// Update the tray menu check marks from the current settings. Called when the settings change
    /// from anywhere: the tray menu itself, the animal's context menu, the mouse wheel or a double click.
    /// </summary>
    public void SyncTrayMenu()
    {
        if (_settings == null)
        {
            return;
        }

        foreach (var item in GetTrayMenuItems())
        {
            if (FurSetting.Equals(item.CommandParameter))
            {
                item.IsEnabled = _settings.Animal.HasFurColors();
                item.Header = _settings.Animal.GetFurMenuHeader();
                continue;
            }

            if (item.CommandParameter is CatColorScheme or DogCoat or ParrotPlumage)
            {
                item.IsVisible = _settings.Animal.HasFurOption(item.CommandParameter);
            }

            item.IsChecked = item.CommandParameter switch
            {
                AnimalKind kind => kind == _settings.Animal,
                AnimalSpeed speed => speed == _settings.Speed,
                CatColorScheme colorScheme => colorScheme == _settings.ColorScheme,
                DogCoat coat => coat == _settings.DogCoat,
                ParrotPlumage plumage => plumage == _settings.ParrotPlumage,
                AnimalSize size => size == _settings.Size,
                PauseSetting => _settings.Paused,
                AlwaysOnTopSetting => _settings.AlwaysOnTop,
                LaunchAtStartupSetting => IsLaunchAtStartupEnabled(),
                _ => item.IsChecked,
            };
        }
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Dispatcher.UIThread.UnhandledException += OnUnhandledException;

            // The animal window is the only window, closing it quits the application.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += OnExit;

            _settings = _settingsStorage.Load();
            _systemAutostart = CreateSystemAutostart();
            var animalWindow = new AnimalWindow(_settings, _systemAutostart);
            animalWindow.Closed += (_, _) => desktop.Shutdown();

            // Save right away: a self-update exits the process without raising the Exit event.
            animalWindow.SettingsChanged += (_, _) => _settingsStorage.Save(_settings);

            // The tray icon needs no separate quit handling: the icons are disposed on dispatcher shutdown.
            AttachTrayIcon(_settings, animalWindow, animalWindow);
            animalWindow.SettingsChanged += (_, _) => SyncTrayMenu();

            desktop.MainWindow = animalWindow;
            animalWindow.Show();

            // Logout and kill send SIGTERM: shut down normally, so the settings are saved
            // and the cursor tracking script is unloaded from the compositor.
            _terminationRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
            {
                context.Cancel = true;
                Dispatcher.UIThread.Post(() => desktop.Shutdown());
            });
        }

        base.OnFrameworkInitializationCompleted();
    }

    private IEnumerable<NativeMenuItem> GetTrayMenuItems()
    {
        foreach (var trayIcon in TrayIcon.GetIcons(this) ?? [])
        {
            foreach (var item in trayIcon.Menu?.Items.OfType<NativeMenuItem>() ?? [])
            {
                yield return item;
                foreach (var child in item.Menu?.Items.OfType<NativeMenuItem>() ?? [])
                {
                    yield return child;
                }
            }
        }
    }

    // Refresh the check marks every time the menu is about to open, like the animal's menu does.
    private void OnTrayMenuOpening(object? sender, EventArgs e) => SyncTrayMenu();

    private void OnPauseClick(object? sender, EventArgs e) => _commands?.TogglePause();

    private void OnAnimalClick(object? sender, EventArgs e)
    {
        if (sender is NativeMenuItem { CommandParameter: AnimalKind kind })
        {
            _appCommands?.SetAnimal(kind);
        }
    }

    private void OnSpeedClick(object? sender, EventArgs e)
    {
        if (sender is NativeMenuItem { CommandParameter: AnimalSpeed speed })
        {
            _commands?.SetSpeed(speed);
        }
    }

    private void OnColorSchemeClick(object? sender, EventArgs e)
    {
        switch ((sender as NativeMenuItem)?.CommandParameter)
        {
            case CatColorScheme colorScheme:
                _commands?.SetFur(colorScheme);
                break;
            case DogCoat coat:
                _commands?.SetDogCoat(coat);
                break;
            case ParrotPlumage plumage:
                _commands?.SetParrotPlumage(plumage);
                break;
        }
    }

    private void OnSizeClick(object? sender, EventArgs e)
    {
        if (sender is NativeMenuItem { CommandParameter: AnimalSize size })
        {
            _commands?.SetSize(size);
        }
    }

    private void OnTopmostClick(object? sender, EventArgs e) => _appCommands?.ToggleAlwaysOnTop();

    private async void OnLaunchAtStartupClick(object? sender, EventArgs e)
    {
        if (_appCommands != null)
        {
            await _appCommands.ToggleLaunchAtStartupAsync();
        }
    }

    private void OnRecenterClick(object? sender, EventArgs e) => _appCommands?.Recenter();

    private async void OnCheckUpdatesClick(object? sender, EventArgs e)
    {
        if (_appCommands != null)
        {
            await _appCommands.CheckForUpdatesAsync();
        }
    }

    private void OnAboutClick(object? sender, EventArgs e) => _appCommands?.ShowAbout();

    private void OnQuitClick(object? sender, EventArgs e) => _appCommands?.Quit();

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _terminationRegistration?.Dispose();
        if (_settings != null)
        {
            _settingsStorage.Save(_settings);
        }
    }

    private void OnUnhandledException(object? sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // A desktop pet should not take the whole session down with it.
        Console.Error.WriteLine($"Catomania: {e.Exception}");
        e.Handled = true;
    }

    private bool IsLaunchAtStartupEnabled()
        => _systemAutostart?.IsLaunchAtStartupEnabledAsync().GetAwaiter().GetResult() ?? false;

    private static ISystemAutostart? CreateSystemAutostart()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxSystemAutostart();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new MacSystemAutostart();
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsSystemAutostart();
        }

        return null;
    }
}
