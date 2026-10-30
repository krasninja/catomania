namespace Catomania.Models;

/// <summary>
/// An immutable snapshot of the animal used to render a frame.
/// </summary>
/// <param name="State">Animation state.</param>
/// <param name="FacingRight">Whether the animal looks to the right.</param>
/// <param name="Phase">Animation phase in radians, used for legs and tail cycles.</param>
/// <param name="IsBlinking">Whether the eyes are closed for a blink.</param>
/// <param name="Trick">Trick that is playing.</param>
/// <param name="TrickProgress">Progress of the trick from 0 to 1.</param>
public readonly record struct AnimalRenderState(
    AnimalAnimationState State,
    bool FacingRight,
    double Phase,
    bool IsBlinking,
    AnimalTrick Trick = AnimalTrick.None,
    double TrickProgress = 0)
{
    /// <summary>
    /// Gets the state used before the first simulation tick.
    /// </summary>
    public static AnimalRenderState Default => new(AnimalAnimationState.Idle, true, 0, false);
}
