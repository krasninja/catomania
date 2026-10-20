using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Services;

/// <summary>
/// The window the animal lives in, as seen by <see cref="AnimalChaseService" />.
/// </summary>
public interface IAnimalHost
{
    /// <summary>
    /// Gets the screen scaling of the window.
    /// </summary>
    double Scaling { get; }

    /// <summary>
    /// Gets a value indicating whether the user is dragging the window.
    /// </summary>
    bool IsDragging { get; }

    /// <summary>
    /// Gets the window size in physical screen pixels.
    /// </summary>
    ScreenPoint SpriteSize { get; }

    /// <summary>
    /// Get the window center in physical screen pixels.
    /// </summary>
    /// <returns>Window center.</returns>
    ScreenPoint GetCenter();

    /// <summary>
    /// Move the window so that its center is at the position.
    /// </summary>
    /// <param name="center">Window center in physical screen pixels.</param>
    void SetCenter(ScreenPoint center);

    /// <summary>
    /// Get the areas the animal is allowed to walk in, in physical screen pixels.
    /// </summary>
    /// <returns>Work areas of all screens.</returns>
    IReadOnlyList<SimulationRect> GetWorkAreas();

    /// <summary>
    /// Draw the animal in the given state.
    /// </summary>
    /// <param name="state">Render state.</param>
    void ApplyRenderState(AnimalRenderState state);
}
