using System.Text.Json.Serialization;

namespace Catomania.Models;

/// <summary>
/// How fast the cat chases the cursor.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CatSpeed>))]
public enum CatSpeed
{
    /// <summary>
    /// A lazy kitty.
    /// </summary>
    Slow,

    /// <summary>
    /// Default speed.
    /// </summary>
    Normal,

    /// <summary>
    /// A kitty in a hurry.
    /// </summary>
    Fast,
}
