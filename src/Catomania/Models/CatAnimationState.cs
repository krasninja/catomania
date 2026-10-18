namespace Catomania.Models;

/// <summary>
/// Cat animation state.
/// </summary>
public enum CatAnimationState
{
    /// <summary>
    /// The cat stays near the cursor, wags its tail and blinks.
    /// </summary>
    Idle,

    /// <summary>
    /// The cat walks towards the cursor.
    /// </summary>
    Walk,

    /// <summary>
    /// The cat runs towards the cursor.
    /// </summary>
    Run,

    /// <summary>
    /// The cat has fallen asleep because the cursor has not moved for a while.
    /// </summary>
    Sleep,

    /// <summary>
    /// The cat is being dragged by the user.
    /// </summary>
    Dragged,
}
