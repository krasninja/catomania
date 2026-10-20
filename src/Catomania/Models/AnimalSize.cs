using System.Text.Json.Serialization;

namespace Catomania.Models;

/// <summary>
/// Animal sprite size.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AnimalSize>))]
public enum AnimalSize
{
    /// <summary>
    /// The smallest size.
    /// </summary>
    Small,

    /// <summary>
    /// Default size.
    /// </summary>
    Medium,

    /// <summary>
    /// The largest size.
    /// </summary>
    Large,
}
