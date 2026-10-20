namespace Catomania.Models;

/// <summary>
/// Extension methods for <see cref="AnimalSize" />.
/// </summary>
public static class AnimalSizeExtensions
{
    /// <summary>
    /// Convert the size to the window size in device independent pixels.
    /// </summary>
    /// <param name="size">Animal size.</param>
    /// <returns>Window size in DIP.</returns>
    public static double ToLogicalSize(this AnimalSize size) => size switch
    {
        AnimalSize.Small => 72,
        AnimalSize.Large => 152,
        _ => 104,
    };
}
