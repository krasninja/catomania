using Catomania.Models;

namespace Catomania.Abstractions;

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
    /// Make the animal say its sound.
    /// </summary>
    void Say();
}
