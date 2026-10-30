using System.Text.Json.Serialization;

namespace Catomania.Models;

/// <summary>
/// Dog coat colors, the patches on the white coat of a springer spaniel.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DogCoat>))]
public enum DogCoat
{
    /// <summary>
    /// Liver and white.
    /// </summary>
    Liver,

    /// <summary>
    /// Black and white.
    /// </summary>
    Black,

    /// <summary>
    /// Red and white.
    /// </summary>
    Red,

    /// <summary>
    /// Lemon and white.
    /// </summary>
    Lemon,
}
