namespace Catomania.Models;

/// <summary>
/// An immutable snapshot of the cat used to render a frame.
/// </summary>
/// <param name="State">Animation state.</param>
/// <param name="FacingRight">Whether the cat looks to the right.</param>
/// <param name="Phase">Animation phase in radians, used for legs and tail cycles.</param>
/// <param name="IsBlinking">Whether the eyes are closed for a blink.</param>
public readonly record struct CatRenderState(CatAnimationState State, bool FacingRight, double Phase, bool IsBlinking)
{
    /// <summary>
    /// Gets the state used before the first simulation tick.
    /// </summary>
    public static CatRenderState Default => new(CatAnimationState.Idle, true, 0, false);
}
