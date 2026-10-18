using System.Text.Json.Serialization;

namespace Catomania.Models;

/// <summary>
/// Cat fur color scheme.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CatColorScheme>))]
public enum CatColorScheme
{
    /// <summary>
    /// Ginger tabby.
    /// </summary>
    Ginger,

    /// <summary>
    /// Gray tabby.
    /// </summary>
    Gray,

    /// <summary>
    /// Black cat.
    /// </summary>
    Black,

    /// <summary>
    /// Cream cat.
    /// </summary>
    Cream,
}
