using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Catomania.Controls;
using Catomania.Models;
using Catomania.Services;

namespace Catomania.Views;

/// <summary>
/// The system tray icon of Catomania. It shows the same options as the cat's context menu,
/// so the cat can be controlled even when it is out of reach or paused. The icon is the same
/// vector drawing the cat window shows, rendered once with a transparent background.
/// </summary>
public sealed class CatTrayIcon : TrayIcon
{
    private const int IconSize = 128;

    private readonly AppSettings _settings;
    private readonly NativeMenuItem _pauseMenuItem = new()
    {
        Header = "Pause",
        ToggleType = MenuItemToggleType.CheckBox,
    };

    private readonly NativeMenuItem _topmostMenuItem = new()
    {
        Header = "Always on Top",
        ToggleType = MenuItemToggleType.CheckBox,
    };

    private readonly List<(NativeMenuItem Item, object Value)> _choiceMenuItems = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CatTrayIcon" /> class.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    /// <param name="commands">The commands to run when the menu items are clicked.</param>
    public CatTrayIcon(AppSettings settings, ICatCommands commands)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        ArgumentNullException.ThrowIfNull(commands);

        ToolTipText = "Catomania";
        IsVisible = true;

        using (var iconStream = AssetLoader.Open(new Uri("avares://Catomania/Assets/Icon.ico")))
        {
            Icon = new WindowIcon(iconStream);
        }

        _pauseMenuItem.Click += (_, _) => commands.TogglePause();
        _topmostMenuItem.Click += (_, _) => commands.ToggleAlwaysOnTop();

        var menu = new NativeMenu();
        menu.Add(_pauseMenuItem);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(CreateChoiceMenu<CatSpeed>("Speed", commands.SetSpeed));
        menu.Add(CreateChoiceMenu<CatColorScheme>("Fur", commands.SetFur));
        menu.Add(CreateChoiceMenu<CatSize>("Size", commands.SetSize));
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(_topmostMenuItem);

        var recenterMenuItem = new NativeMenuItem { Header = "Recenter" };
        recenterMenuItem.Click += (_, _) => commands.Recenter();
        menu.Add(recenterMenuItem);

        var checkUpdatesMenuItem = new NativeMenuItem { Header = "Check for Updates" };
        checkUpdatesMenuItem.Click += async (_, _) => await commands.CheckForUpdatesAsync();
        menu.Add(checkUpdatesMenuItem);

        menu.Add(new NativeMenuItemSeparator());
        var quitMenuItem = new NativeMenuItem { Header = "Quit Catomania" };
        quitMenuItem.Click += (_, _) => commands.Quit();
        menu.Add(quitMenuItem);

        // Refresh the check marks every time the menu is about to open, like the cat's menu does.
        menu.Opening += (_, _) => SyncMenu();
        Menu = menu;
        SyncMenu();
    }

    /// <summary>
    /// Update the menu check marks from the current settings. Called when the settings change
    /// from anywhere: the tray menu itself, the cat's context menu, the mouse wheel or a double click.
    /// </summary>
    public void SyncMenu()
    {
        _pauseMenuItem.IsChecked = _settings.Paused;
        _topmostMenuItem.IsChecked = _settings.AlwaysOnTop;
        foreach (var (item, value) in _choiceMenuItems)
        {
            item.IsChecked = value switch
            {
                CatSpeed speed => speed == _settings.Speed,
                CatColorScheme colorScheme => colorScheme == _settings.ColorScheme,
                CatSize size => size == _settings.Size,
                _ => item.IsChecked,
            };
        }
    }

    /// <summary>
    /// Create a submenu with one radio item per value of an enum, in declaration order.
    /// </summary>
    /// <typeparam name="T">Enum of the possible values.</typeparam>
    /// <param name="header">Submenu header.</param>
    /// <param name="apply">The command to run when a value is picked.</param>
    /// <returns>The submenu item.</returns>
    private NativeMenuItem CreateChoiceMenu<T>(string header, Action<T> apply)
        where T : struct, Enum
    {
        var submenu = new NativeMenu();
        foreach (T value in Enum.GetValues<T>())
        {
            var item = new NativeMenuItem
            {
                Header = value.ToString(),
                ToggleType = MenuItemToggleType.Radio,
            };
            item.Click += (_, _) => apply(value);
            _choiceMenuItems.Add((item, value));
            submenu.Add(item);
        }

        return new NativeMenuItem { Header = header, Menu = submenu };
    }
}
