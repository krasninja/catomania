using System.Text.Json.Serialization;

namespace Catomania.Models;

/// <summary>
/// The animal that chases the cursor.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AnimalKind>))]
public enum AnimalKind
{
    /// <summary>
    /// A cat.
    /// </summary>
    Cat,

    /// <summary>
    /// A panda cub.
    /// </summary>
    Panda,

    /// <summary>
    /// A springer spaniel.
    /// </summary>
    Dog,

    /// <summary>
    /// A mint snake with a leaf on its head.
    /// </summary>
    Snake,

    /// <summary>
    /// A scarlet macaw.
    /// </summary>
    Parrot,
}
