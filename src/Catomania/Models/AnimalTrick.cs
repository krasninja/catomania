namespace Catomania.Models;

/// <summary>
/// A short random animation the animal plays from time to time while chasing is paused.
/// </summary>
public enum AnimalTrick
{
    /// <summary>
    /// No trick is playing.
    /// </summary>
    None,

    /// <summary>
    /// The animal wanders a few short legs in random directions, stopping between them, and walks back.
    /// </summary>
    Walk,

    /// <summary>
    /// The animal turns from side to side, looking around.
    /// </summary>
    LookAround,

    /// <summary>
    /// The animal crouches and hops up in place.
    /// </summary>
    Hop,

    /// <summary>
    /// The animal makes its sound.
    /// </summary>
    Speak,
}
