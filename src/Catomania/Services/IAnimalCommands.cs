using Catomania.Models;

namespace Catomania.Services;

/// <summary>
/// The commands the user can run on the animal, shared by the animal's context menu
/// and the system tray icon menu.
/// </summary>
public interface IAnimalCommands
{
    /// <summary>
    /// Toggle the animal pause.
    /// </summary>
    void TogglePause();

    /// <summary>
    /// Set the chase speed.
    /// </summary>
    /// <param name="speed">Chase speed.</param>
    void SetSpeed(AnimalSpeed speed);

    /// <summary>
    /// Set the animal that chases the cursor.
    /// </summary>
    /// <param name="kind">Animal kind.</param>
    void SetAnimal(AnimalKind kind);

    /// <summary>
    /// Set the fur color scheme.
    /// </summary>
    /// <param name="colorScheme">Fur color scheme.</param>
    void SetFur(CatColorScheme colorScheme);

    /// <summary>
    /// Set the animal size.
    /// </summary>
    /// <param name="size">Animal size.</param>
    void SetSize(AnimalSize size);

    /// <summary>
    /// Toggle keeping the animal above other windows.
    /// </summary>
    void ToggleAlwaysOnTop();

    /// <summary>
    /// Move the animal back to the cursor.
    /// </summary>
    void Recenter();

    /// <summary>
    /// Make the animal say its sound.
    /// </summary>
    void Say();

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
