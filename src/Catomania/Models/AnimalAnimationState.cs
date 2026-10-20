namespace Catomania.Models;

/// <summary>
/// Animal animation state.
/// </summary>
public enum AnimalAnimationState
{
    /// <summary>
    /// The animal stays near the cursor, wags its tail and blinks.
    /// </summary>
    Idle,

    /// <summary>
    /// The animal walks towards the cursor.
    /// </summary>
    Walk,

    /// <summary>
    /// The animal runs towards the cursor.
    /// </summary>
    Run,

    /// <summary>
    /// The animal has fallen asleep because the cursor has not moved for a while.
    /// </summary>
    Sleep,

    /// <summary>
    /// The animal is being dragged by the user.
    /// </summary>
    Dragged,
}
