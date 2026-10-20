using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Catomania.Models;
using Catomania.Services;
using Catomania.Views;

namespace Catomania;

/// <summary>
/// Avalonia <see cref="Application" /> class.
/// </summary>
public sealed partial class App : Application
{
    private readonly SettingsStorage _settingsStorage = new();

    private AppSettings? _settings;
    private PosixSignalRegistration? _terminationRegistration;

    /// <summary>
    /// Initializes a new instance of the <see cref="App" /> class.
    /// </summary>
    public App()
    {
        Dispatcher.UIThread.UnhandledException += OnUnhandledException;
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // The cat window is the only window, closing it quits the application.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += OnExit;

            _settings = _settingsStorage.Load();
            var catWindow = new CatWindow(_settings);
            catWindow.Closed += (_, _) => desktop.Shutdown();

            // Save right away: a self-update exits the process without raising the Exit event.
            catWindow.SettingsChanged += (_, _) => _settingsStorage.Save(_settings);

            // The tray icon shows the same options as the cat's context menu. It needs no
            // separate quit handling: the icons are disposed on dispatcher shutdown.
            var trayIcon = new CatTrayIcon(_settings, catWindow);
            catWindow.SettingsChanged += (_, _) => trayIcon.SyncMenu();
            TrayIcon.SetIcons(this, new TrayIcons { trayIcon });

            desktop.MainWindow = catWindow;
            catWindow.Show();

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
}
