using Catomania.Abstractions;
using Catomania.Simulation;

namespace Catomania.Input;

/// <summary>
/// Creates the cursor position provider suitable for the current platform.
/// </summary>
public static class CursorPositionProviderFactory
{
    /// <summary>
    /// Create the provider that uses the platform API when it is possible and falls back
    /// to the position reported by the animal window pointer events.
    /// </summary>
    /// <param name="fallbackProvider">Provider that receives positions reported by the window.</param>
    /// <param name="scalingProvider">Provides the screen scaling, used on macOS.</param>
    /// <param name="screenBoundsProvider">Provides the bounds of all screens in physical pixels, used on Wayland.</param>
    /// <returns>Cursor position provider.</returns>
    public static CompositeCursorPositionProvider CreateDefault(
        FallbackCursorPositionProvider fallbackProvider,
        Func<double>? scalingProvider = null,
        Func<SimulationRect>? screenBoundsProvider = null)
    {
        var providers = new List<ICursorPositionProvider>();
        if (OperatingSystem.IsWindows())
        {
            providers.Add(new Win32CursorPositionProvider());
        }
        else if (OperatingSystem.IsMacOS())
        {
            providers.Add(new MacOsCursorPositionProvider(scalingProvider));
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
        {
            // On Wayland XWayland knows the cursor position only while it is over the animal window,
            // ask the compositor first.
            if (LinuxDesktop.IsWaylandSession && LinuxDesktop.IsCurrentDesktop(LinuxDesktop.Kde))
            {
                providers.Add(new KWinCursorPositionProvider(screenBoundsProvider));
            }
            else if (LinuxDesktop.IsWaylandSession && LinuxDesktop.IsCurrentDesktop(LinuxDesktop.Gnome))
            {
                providers.Add(new GnomeShellCursorPositionProvider(screenBoundsProvider));
            }

            providers.Add(new X11CursorPositionProvider());
        }

        providers.Add(fallbackProvider);
        return new CompositeCursorPositionProvider(providers);
    }
}
