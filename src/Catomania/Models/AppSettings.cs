namespace Catomania.Models;

/// <summary>
/// Application settings, persisted between runs.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets the chase speed.
    /// </summary>
    public CatSpeed Speed { get; set; } = CatSpeed.Normal;

    /// <summary>
    /// Gets or sets the fur color scheme.
    /// </summary>
    public CatColorScheme ColorScheme { get; set; } = CatColorScheme.Ginger;

    /// <summary>
    /// Gets or sets the cat size.
    /// </summary>
    public CatSize Size { get; set; } = CatSize.Medium;

    /// <summary>
    /// Gets or sets a value indicating whether the window is kept on top of other windows.
    /// </summary>
    public bool AlwaysOnTop { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the cat is paused and does not chase the cursor.
    /// </summary>
    public bool Paused { get; set; }

    /// <summary>
    /// Gets or sets the number of seconds of cursor inactivity before the cat falls asleep.
    /// </summary>
    public int SleepAfterSeconds { get; set; } = 30;
}
