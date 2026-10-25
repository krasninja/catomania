using Avalonia.Controls;
using Avalonia.Headless;
using Catomania.Models;
using Catomania.Services;
using Xunit;

namespace Catomania.Tests;

/// <summary>
/// Tests for the tray menu declared in <see cref="App" />: the menu shows the same options as the animal's
/// context menu, the items run the shared commands and the check marks follow the settings.
/// </summary>
public sealed class AppTrayMenuTests
{
    private static readonly HeadlessUnitTestSession _session = TestHeadlessSession.Instance;

    [Fact]
    public Task Menu_MirrorsCatContextMenu()
        => _session.Dispatch(() =>
        {
            var trayIcon = CreateTrayIcon(new AppSettings(), new RecordingCommands());

            var items = GetTopLevelItems(trayIcon);
            Assert.Equal(
                new[] { "Pause", "Animal", "Speed", "Fur", "Size", "Always on Top", "Recenter", "Check for Updates", "Quit Catomania" },
                items.Select(item => item.Header));
            Assert.Equal(3, trayIcon.Menu!.Items.OfType<NativeMenuItemSeparator>().Count());

            Assert.Equal(MenuItemToggleType.CheckBox, GetItem(items, "Pause").ToggleType);
            Assert.Equal(MenuItemToggleType.CheckBox, GetItem(items, "Always on Top").ToggleType);

            Assert.Equal(
                new[] { "Cat", "Panda", "Dog" },
                GetChildren(items, "Animal").Select(item => item.Header));
            Assert.Equal(
                new[] { "Slow", "Normal", "Fast" },
                GetChildren(items, "Speed").Select(item => item.Header));
            Assert.Equal(
                new[] { "Ginger", "Gray", "Black", "Cream" },
                GetChildren(items, "Fur").Select(item => item.Header));
            Assert.Equal(
                new[] { "Small", "Medium", "Large" },
                GetChildren(items, "Size").Select(item => item.Header));
            Assert.All(
                GetChildren(items, "Animal").Concat(GetChildren(items, "Speed")).Concat(GetChildren(items, "Fur")).Concat(GetChildren(items, "Size")),
                item => Assert.Equal(MenuItemToggleType.Radio, item.ToggleType));
        }, CancellationToken.None);

    [Fact]
    public Task Click_EveryOption_RunsItsCommand()
        => _session.Dispatch(() =>
        {
            var commands = new RecordingCommands();
            var trayIcon = CreateTrayIcon(new AppSettings(), commands);
            var items = GetTopLevelItems(trayIcon);

            RaiseClick(GetItem(items, "Pause"));
            RaiseClick(GetItem(items, "Always on Top"));
            RaiseClick(GetItem(items, "Recenter"));
            RaiseClick(GetItem(items, "Check for Updates"));
            RaiseClick(GetItem(items, "Quit Catomania"));
            RaiseClick(GetChild(items, "Animal", "Panda"));
            RaiseClick(GetChild(items, "Speed", "Fast"));
            RaiseClick(GetChild(items, "Fur", "Black"));
            RaiseClick(GetChild(items, "Size", "Small"));

            Assert.Equal(
                new[]
                {
                    "pause",
                    "alwaysOnTop",
                    "recenter",
                    "checkForUpdates",
                    "quit",
                    "animal:Panda",
                    "speed:Fast",
                    "fur:Black",
                    "size:Small",
                },
                commands.Calls);
        }, CancellationToken.None);

    [Fact]
    public Task SyncMenu_ChecksCurrentSettings()
        => _session.Dispatch(() =>
        {
            var settings = new AppSettings
            {
                Paused = true,
                AlwaysOnTop = false,
                Speed = AnimalSpeed.Fast,
                ColorScheme = CatColorScheme.Black,
                Size = AnimalSize.Large,
            };
            var app = new App();
            var trayIcon = CreateTrayIcon(app, settings, new RecordingCommands());
            var items = GetTopLevelItems(trayIcon);

            Assert.True(GetItem(items, "Pause").IsChecked);
            Assert.False(GetItem(items, "Always on Top").IsChecked);
            Assert.True(GetItem(items, "Fur").IsEnabled);

            settings.Paused = false;
            settings.AlwaysOnTop = true;
            settings.Speed = AnimalSpeed.Slow;
            settings.ColorScheme = CatColorScheme.Cream;
            settings.Size = AnimalSize.Small;
            settings.Animal = AnimalKind.Panda;
            app.SyncTrayMenu();

            Assert.False(GetItem(items, "Pause").IsChecked);
            Assert.True(GetItem(items, "Always on Top").IsChecked);

            // The fur colors are of the cat only.
            Assert.False(GetItem(items, "Fur").IsEnabled);
            foreach (var item in GetChildren(items, "Animal").Concat(GetChildren(items, "Speed")).Concat(GetChildren(items, "Fur")).Concat(GetChildren(items, "Size")))
            {
                var isChecked = item.Header switch
                {
                    "Panda" => true,
                    "Slow" => true,
                    "Cream" => true,
                    "Small" => true,
                    _ => false,
                };
                Assert.Equal(isChecked, item.IsChecked);
            }
        }, CancellationToken.None);

    private static TrayIcon CreateTrayIcon(AppSettings settings, IAnimalCommands commands)
        => CreateTrayIcon(new App(), settings, commands);

    private static TrayIcon CreateTrayIcon(App app, AppSettings settings, IAnimalCommands commands)
    {
        app.Initialize();
        app.AttachTrayIcon(settings, commands);
        var trayIcon = Assert.Single(TrayIcon.GetIcons(app)!);
        Assert.True(trayIcon.IsVisible);
        return trayIcon;
    }

    private static List<NativeMenuItem> GetTopLevelItems(TrayIcon trayIcon)
        => trayIcon.Menu!.Items.OfType<NativeMenuItem>().Where(item => item is not NativeMenuItemSeparator).ToList();

    private static List<NativeMenuItem> GetChildren(List<NativeMenuItem> items, string header)
        => GetItem(items, header).Menu!.Items.OfType<NativeMenuItem>().ToList();

    private static NativeMenuItem GetItem(List<NativeMenuItem> items, string header)
        => items.Single(item => item.Header == header);

    private static NativeMenuItem GetChild(List<NativeMenuItem> items, string header, string childHeader)
        => GetChildren(items, header).Single(item => item.Header == childHeader);

    private static void RaiseClick(NativeMenuItem item)
    {
        // NativeMenuItem raises Click only through the native menu exporter bridge, which is
        // what the tray icon platform implementations call when the user clicks a menu item.
        var bridge = item.GetType().GetInterface("Avalonia.Controls.INativeMenuItemExporterEventsImplBridge")
            ?? throw new InvalidOperationException("The native menu item bridge interface is not found.");
        bridge.GetMethod("RaiseClicked")?.Invoke(item, null);
    }

    private sealed class RecordingCommands : IAnimalCommands
    {
        public List<string> Calls { get; } = new();

        public void TogglePause() => Calls.Add("pause");

        public void SetAnimal(AnimalKind kind) => Calls.Add($"animal:{kind}");

        public void SetSpeed(AnimalSpeed speed) => Calls.Add($"speed:{speed}");

        public void SetFur(CatColorScheme colorScheme) => Calls.Add($"fur:{colorScheme}");

        public void SetSize(AnimalSize size) => Calls.Add($"size:{size}");

        public void ToggleAlwaysOnTop() => Calls.Add("alwaysOnTop");

        public void Recenter() => Calls.Add("recenter");

        public Task CheckForUpdatesAsync()
        {
            Calls.Add("checkForUpdates");
            return Task.CompletedTask;
        }

        public void Quit() => Calls.Add("quit");
    }
}
