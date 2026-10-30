namespace Catomania.Models;

/// <summary>
/// Extension methods for <see cref="AnimalKind" />, used by the "Fur" menus: every animal with a choice
/// of colors has its own options, and the menu shows the options of the current animal only.
/// </summary>
public static class AnimalKindExtensions
{
    /// <summary>
    /// Check whether the animal comes in a choice of colors.
    /// </summary>
    /// <param name="kind">Animal kind.</param>
    /// <returns>True if the animal has colors to choose from.</returns>
    public static bool HasFurColors(this AnimalKind kind) => kind is AnimalKind.Cat or AnimalKind.Dog or AnimalKind.Parrot;

    /// <summary>
    /// Get the header of the menu with the animal's colors.
    /// </summary>
    /// <param name="kind">Animal kind.</param>
    /// <returns>Menu header.</returns>
    public static string GetFurMenuHeader(this AnimalKind kind) => kind == AnimalKind.Parrot ? "Feathers" : "Fur";

    /// <summary>
    /// Check whether a color option of the "Fur" menu belongs to the animal.
    /// </summary>
    /// <param name="kind">Animal kind.</param>
    /// <param name="option">Color option: a <see cref="CatColorScheme" />, a <see cref="DogCoat" /> or a <see cref="ParrotPlumage" />.</param>
    /// <returns>True if the option colors the animal.</returns>
    public static bool HasFurOption(this AnimalKind kind, object option) => option switch
    {
        CatColorScheme => kind == AnimalKind.Cat,
        DogCoat => kind == AnimalKind.Dog,
        ParrotPlumage => kind == AnimalKind.Parrot,
        _ => false,
    };
}
