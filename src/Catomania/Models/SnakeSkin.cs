using System.Text.Json.Serialization;

namespace Catomania.Models;

/// <summary>
/// Snake skin colors.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SnakeSkin>))]
public enum SnakeSkin
{
    /// <summary>
    /// Mint green with a cream belly.
    /// </summary>
    Mint,

    /// <summary>
    /// Swampy dark green with a muddy belly.
    /// </summary>
    DarkGreen,

    /// <summary>
    /// Blue with a pale blue belly.
    /// </summary>
    Blue,

    /// <summary>
    /// Pink with a pale pink belly.
    /// </summary>
    Pink,

    /// <summary>
    /// Yellow with a cream belly.
    /// </summary>
    Yellow,
}
