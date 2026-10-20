using System.Text.Json.Serialization;

namespace Catomania.Models;

/// <summary>
/// How fast the animal chases the cursor.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AnimalSpeed>))]
public enum AnimalSpeed
{
    /// <summary>
    /// A lazy walk.
    /// </summary>
    Slow,

    /// <summary>
    /// Default speed.
    /// </summary>
    Normal,

    /// <summary>
    /// In a hurry.
    /// </summary>
    Fast,
}
