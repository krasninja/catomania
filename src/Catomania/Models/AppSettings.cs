namespace Catomania.Models;

/// <summary>
/// Application settings, persisted between runs.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets the animal that chases the cursor.
    /// </summary>
    public AnimalKind Animal { get; set; } = AnimalKind.Cat;

    /// <summary>
    /// Gets or sets the chase speed.
    /// </summary>
    public AnimalSpeed Speed { get; set; } = AnimalSpeed.Normal;

    /// <summary>
    /// Gets or sets the fur color scheme.
    /// </summary>
    public CatColorScheme ColorScheme { get; set; } = CatColorScheme.Ginger;

    /// <summary>
    /// Gets or sets the animal size.
    /// </summary>
    public AnimalSize Size { get; set; } = AnimalSize.Medium;

    /// <summary>
    /// Gets or sets a value indicating whether the window is kept on top of other windows.
    /// </summary>
    public bool AlwaysOnTop { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the animal is paused and does not chase the cursor.
    /// </summary>
    public bool Paused { get; set; }

    /// <summary>
    /// Gets or sets the number of seconds of cursor inactivity before the animal falls asleep.
    /// </summary>
    public int SleepAfterSeconds { get; set; } = 30;
}
