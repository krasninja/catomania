using Catomania.Models;

namespace Catomania.Abstractions;

/// <summary>
/// The application-level commands shared by the animal's context menu and the system tray icon menu.
/// </summary>
public interface IAppCommands
{
    /// <summary>
    /// Set the animal that chases the cursor.
    /// </summary>
    /// <param name="kind">Animal kind.</param>
    void SetAnimal(AnimalKind kind);

    /// <summary>
    /// Toggle keeping the animal above other windows.
    /// </summary>
    void ToggleAlwaysOnTop();

    /// <summary>
    /// Move the animal back to the cursor.
    /// </summary>
    void Recenter();

    /// <summary>
    /// Toggle launching the application at system startup.
    /// </summary>
    /// <returns>A task that completes when the toggle is done.</returns>
    Task ToggleLaunchAtStartupAsync();

    /// <summary>
    /// Check the release feed for updates, apply them and show a report.
    /// </summary>
    /// <returns>A task that completes when the check is done.</returns>
    Task CheckForUpdatesAsync();

    /// <summary>
    /// Show the "About" dialog with the application name, platform and version.
    /// </summary>
    void ShowAbout();

    /// <summary>
    /// Quit the application.
    /// </summary>
    void Quit();
}
