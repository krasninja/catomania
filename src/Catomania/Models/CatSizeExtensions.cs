namespace Catomania.Models;

/// <summary>
/// Extension methods for <see cref="CatSize" />.
/// </summary>
public static class CatSizeExtensions
{
    /// <summary>
    /// Convert the size to the window size in device independent pixels.
    /// </summary>
    /// <param name="size">Cat size.</param>
    /// <returns>Window size in DIP.</returns>
    public static double ToLogicalSize(this CatSize size) => size switch
    {
        CatSize.Small => 72,
        CatSize.Large => 152,
        _ => 104,
    };
}
