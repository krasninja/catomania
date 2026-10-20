using System.Text.Json;
using Catomania.Input;
using Catomania.Simulation;
using Xunit;

namespace Catomania.Tests.Input;

/// <summary>
/// Tests for the Wayland compositor cursor position providers.
/// </summary>
public sealed class CompositorCursorPositionProviderTests : IDisposable
{
    private readonly string _directoryPath =
        Path.Combine(Path.GetTempPath(), "catomania-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void MapToScreen_XWaylandScaling_ShouldScaleLogicalCoordinates()
    {
        // Arrange. KDE with two screens at 1.5 and 1.2 scaling, XWayland scales everything by 1.5.
        var logicalBounds = new SimulationRect(0, 0, 3841, 1200);
        var screenBounds = new SimulationRect(0, 0, 5761, 1800);

        // Act. The left edge of the second screen.
        var position = CompositorCursorPositionProvider.MapToScreen(new ScreenPoint(1707, 600), logicalBounds, screenBounds);

        // Assert.
        Assert.InRange(position.X, 2560, 2562);
        Assert.Equal(900, position.Y, 6);
    }

    [Fact]
    public void MapToScreen_ScreenBoundsAreUnknown_ShouldReturnLogicalCoordinates()
    {
        var position = CompositorCursorPositionProvider.MapToScreen(
            new ScreenPoint(100, 200),
            new SimulationRect(0, 0, 1920, 1080),
            SimulationRect.Empty);

        Assert.Equal(new ScreenPoint(100, 200), position);
    }

    [Theory]
    [InlineData("KDE", "KDE", true)]
    [InlineData("ubuntu:GNOME", "GNOME", true)]
    [InlineData("GNOME-Classic:GNOME", "GNOME", true)]
    [InlineData("X-Cinnamon", "GNOME", false)]
    [InlineData(null, "KDE", false)]
    public void IsDesktop_ShouldCheckTheDesktopList(string? currentDesktops, string desktop, bool expected)
    {
        Assert.Equal(expected, LinuxDesktop.IsDesktop(currentDesktops, desktop));
    }

    [Theory]
    [InlineData("wayland", null, true)]
    [InlineData("x11", null, false)]
    [InlineData(null, "wayland-0", true)]
    [InlineData(null, null, false)]
    public void IsWayland_ShouldCheckTheSessionType(string? sessionType, string? waylandDisplay, bool expected)
    {
        Assert.Equal(expected, LinuxDesktop.IsWayland(sessionType, waylandDisplay));
    }

    [Fact]
    public void KWinCreateScript_ShouldReportToTheService()
    {
        var script = KWinCursorPositionProvider.CreateScript(":1.42");

        Assert.Contains("\":1.42\"", script);
        Assert.DoesNotContain("%SERVICE%", script);
        Assert.Contains(KWinCursorPositionProvider.ObjectPath, script);
        Assert.Contains(KWinCursorPositionProvider.InterfaceName, script);
        Assert.Contains(KWinCursorPositionProvider.SetPositionMethod, script);
    }

    [Fact]
    public void GnomeExtensionMetadata_ShouldMatchTheExtensionUuid()
    {
        using var metadata = JsonDocument.Parse(GnomeShellCursorPositionProvider.ReadExtensionFile("metadata.json"));

        Assert.Equal(GnomeShellCursorPositionProvider.ExtensionUuid, metadata.RootElement.GetProperty("uuid").GetString());
    }

    [Fact]
    public void GnomeInstallExtension_ShouldWriteTheFilesOnce()
    {
        // Act.
        var isInstalled = GnomeShellCursorPositionProvider.InstallExtension(_directoryPath);
        var isReinstalled = GnomeShellCursorPositionProvider.InstallExtension(_directoryPath);

        // Assert.
        Assert.True(isInstalled);
        Assert.False(isReinstalled);
        foreach (var fileName in GnomeShellCursorPositionProvider.ExtensionFiles)
        {
            Assert.True(File.Exists(Path.Combine(_directoryPath, fileName)), $"{fileName} is not installed");
        }
    }

    public void Dispose()
    {
        if (Directory.Exists(_directoryPath))
        {
            Directory.Delete(_directoryPath, true);
        }
    }
}
