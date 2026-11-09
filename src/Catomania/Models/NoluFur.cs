using System.Text.Json.Serialization;

namespace Catomania.Models;

/// <summary>
/// Fur colors of Nolu. Each fur color has a matching head, ear and limb color.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<NoluFur>))]
public enum NoluFur
{
    /// <summary>
    /// Brown, like in the original artwork.
    /// </summary>
    Brown,

    /// <summary>
    /// Violet fur with a light violet head.
    /// </summary>
    Violet,

    /// <summary>
    /// Green fur with a light green head.
    /// </summary>
    Green,
}
