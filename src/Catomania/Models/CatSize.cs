using System.Text.Json.Serialization;

namespace Catomania.Models;

/// <summary>
/// Cat sprite size.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CatSize>))]
public enum CatSize
{
    /// <summary>
    /// A kitten.
    /// </summary>
    Small,

    /// <summary>
    /// Default size.
    /// </summary>
    Medium,

    /// <summary>
    /// A chonky cat.
    /// </summary>
    Large,
}
