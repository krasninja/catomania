namespace Catomania.Models;

/// <summary>
/// Extension methods for <see cref="AnimalSpeed" />.
/// </summary>
public static class AnimalSpeedExtensions
{
    /// <summary>
    /// Convert the speed to a simulation speed multiplier.
    /// </summary>
    /// <param name="speed">Animal speed.</param>
    /// <returns>Speed multiplier.</returns>
    public static double ToMultiplier(this AnimalSpeed speed) => speed switch
    {
        AnimalSpeed.Slow => 0.55,
        AnimalSpeed.Fast => 1.75,
        _ => 1.0,
    };
}
