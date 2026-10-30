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

    /// <summary>
    /// The animal shows an emote in the speech bubble: a question mark, a heart, a note.
    /// </summary>
    Emote,

    /// <summary>
    /// The animal rocks from side to side, happy.
    /// </summary>
    Wiggle,

    /// <summary>
    /// The animal shakes itself like a wet dog.
    /// </summary>
    Shake,

    /// <summary>
    /// The animal hops a few times in a random direction and hops back.
    /// </summary>
    BunnyHops,

    /// <summary>
    /// The animal hops and turns around in the air.
    /// </summary>
    Spin,

    /// <summary>
    /// The animal dashes a few legs in random directions at full speed and runs back.
    /// </summary>
    Zoomies,
}
