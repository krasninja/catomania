using System.Text.Json.Serialization;

namespace Catomania.Models;

/// <summary>
/// Parrot plumage, each one a macaw species.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ParrotPlumage>))]
public enum ParrotPlumage
{
    /// <summary>
    /// Scarlet macaw: red with yellow, green and blue wings.
    /// </summary>
    Scarlet,

    /// <summary>
    /// Blue-and-gold macaw: blue back, golden chest and a green forehead.
    /// </summary>
    BlueAndGold,

    /// <summary>
    /// Military macaw: green with a red forehead and blue flight feathers.
    /// </summary>
    Green,

    /// <summary>
    /// Hyacinth macaw: cobalt blue with a yellow eye ring.
    /// </summary>
    Hyacinth,
}
