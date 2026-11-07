using System.Text.Json.Serialization;

namespace Catomania.Models;

/// <summary>
/// Fur colors of Muchi the Cat. The muzzle, the belly and the pink cheeks stay the same.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<MuchiFur>))]
public enum MuchiFur
{
    /// <summary>
    /// Orange, like in the artwork.
    /// </summary>
    Orange,

    /// <summary>
    /// Light gray.
    /// </summary>
    Gray,

    /// <summary>
    /// Charcoal black.
    /// </summary>
    Black,

    /// <summary>
    /// Pale beige.
    /// </summary>
    Beige,

    /// <summary>
    /// Warm brown.
    /// </summary>
    Brown,
}
