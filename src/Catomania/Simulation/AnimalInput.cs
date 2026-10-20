namespace Catomania.Simulation;

/// <summary>
/// Simulation input for a single tick.
/// </summary>
/// <param name="CursorPosition">Current cursor position in physical screen pixels.</param>
/// <param name="Bounds">Area the animal center is allowed to stay in, in physical screen pixels.</param>
/// <param name="SpeedMultiplier">User speed multiplier.</param>
/// <param name="IsDragging">Whether the user is dragging the animal.</param>
/// <param name="IsPaused">Whether chasing is paused.</param>
/// <param name="SpriteSize">Size of the animal design space in physical screen pixels, used to match the gait
/// to the distance travelled. The gait is driven by time when it is not set.</param>
public readonly record struct AnimalInput(
    ScreenPoint CursorPosition,
    SimulationRect Bounds,
    double SpeedMultiplier = 1.0,
    bool IsDragging = false,
    bool IsPaused = false,
    double SpriteSize = 0);
