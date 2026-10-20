using Catomania.Models;

namespace Catomania.Services;

/// <summary>
/// The commands the user can run on the cat, shared by the cat's context menu
/// and the system tray icon menu.
/// </summary>
public interface ICatCommands
{
    /// <summary>
    /// Toggle the cat pause.
    /// </summary>
    void TogglePause();

    /// <summary>
    /// Set the chase speed.
    /// </summary>
    /// <param name="speed">Chase speed.</param>
    void SetSpeed(CatSpeed speed);

    /// <summary>
    /// Set the fur color scheme.
    /// </summary>
    /// <param name="colorScheme">Fur color scheme.</param>
    void SetFur(CatColorScheme colorScheme);

    /// <summary>
    /// Set the cat size.
    /// </summary>
    /// <param name="size">Cat size.</param>
    void SetSize(CatSize size);

    /// <summary>
    /// Toggle keeping the cat above other windows.
    /// </summary>
    void ToggleAlwaysOnTop();

    /// <summary>
    /// Move the cat back to the cursor.
    /// </summary>
    void Recenter();

    /// <summary>
    /// Check the release feed for updates, apply them and show a report.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    Task CheckForUpdatesAsync();

    /// <summary>
    /// Quit the application.
    /// </summary>
    void Quit();
}
