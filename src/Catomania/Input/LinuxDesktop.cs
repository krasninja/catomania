namespace Catomania.Input;

/// <summary>
/// Detects the Linux desktop session the application runs in.
/// </summary>
public static class LinuxDesktop
{
    /// <summary>
    /// KDE Plasma desktop name in <c>XDG_CURRENT_DESKTOP</c>.
    /// </summary>
    public const string Kde = "KDE";

    /// <summary>
    /// GNOME desktop name in <c>XDG_CURRENT_DESKTOP</c>.
    /// </summary>
    public const string Gnome = "GNOME";

    /// <summary>
    /// Gets a value indicating whether the current session is a Wayland one.
    /// </summary>
    public static bool IsWaylandSession => IsWayland(
        Environment.GetEnvironmentVariable("XDG_SESSION_TYPE"),
        Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"));

    /// <summary>
    /// Check whether the current session runs the desktop.
    /// </summary>
    /// <param name="desktop">Desktop name, for example <see cref="Kde" />.</param>
    /// <returns><c>true</c> if the desktop is the current one.</returns>
    public static bool IsCurrentDesktop(string desktop)
        => IsDesktop(Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP"), desktop);

    /// <summary>
    /// Check whether the session is a Wayland one by the environment variable values.
    /// </summary>
    /// <param name="sessionType">Value of <c>XDG_SESSION_TYPE</c>.</param>
    /// <param name="waylandDisplay">Value of <c>WAYLAND_DISPLAY</c>.</param>
    /// <returns><c>true</c> for a Wayland session.</returns>
    public static bool IsWayland(string? sessionType, string? waylandDisplay)
        => string.Equals(sessionType, "wayland", StringComparison.OrdinalIgnoreCase)
           || !string.IsNullOrEmpty(waylandDisplay);

    /// <summary>
    /// Check whether the desktop list contains the desktop.
    /// </summary>
    /// <param name="currentDesktops">Value of <c>XDG_CURRENT_DESKTOP</c>: a colon separated list, for example <c>ubuntu:GNOME</c>.</param>
    /// <param name="desktop">Desktop name.</param>
    /// <returns><c>true</c> if the list contains the desktop.</returns>
    public static bool IsDesktop(string? currentDesktops, string desktop)
        => (currentDesktops ?? string.Empty)
            .Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(desktop, StringComparer.OrdinalIgnoreCase);
}
