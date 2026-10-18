namespace Catomania.Models;

/// <summary>
/// Extension methods for <see cref="CatSpeed" />.
/// </summary>
public static class CatSpeedExtensions
{
    /// <summary>
    /// Convert the speed to a simulation speed multiplier.
    /// </summary>
    /// <param name="speed">Cat speed.</param>
    /// <returns>Speed multiplier.</returns>
    public static double ToMultiplier(this CatSpeed speed) => speed switch
    {
        CatSpeed.Slow => 0.55,
        CatSpeed.Fast => 1.75,
        _ => 1.0,
    };
}
