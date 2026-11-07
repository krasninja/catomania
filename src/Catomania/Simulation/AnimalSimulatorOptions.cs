using System.Collections.ObjectModel;
using Catomania.Models;

namespace Catomania.Simulation;

/// <summary>
/// Tunable parameters of <see cref="AnimalSimulator" />. All speeds are in physical pixels per second.
/// </summary>
public sealed class AnimalSimulatorOptions
{
    private static readonly AnimalTrick[] DefaultTricks =
        [
            AnimalTrick.Walk,
            AnimalTrick.LookAround,
            AnimalTrick.Hop,
            AnimalTrick.Speak,
            AnimalTrick.Emote,
            AnimalTrick.Wiggle,
            AnimalTrick.Shake,
            AnimalTrick.BunnyHops,
            AnimalTrick.Spin,
            AnimalTrick.Zoomies,
            AnimalTrick.Happy
        ];

    /// <summary>
    /// Gets or sets the walking speed.
    /// </summary>
    public double WalkSpeed { get; set; } = 120;

    /// <summary>
    /// Gets or sets the running speed.
    /// </summary>
    public double RunSpeed { get; set; } = 430;

    /// <summary>
    /// Gets or sets the distance covered during one walking gait cycle, as a fraction of the sprite size.
    /// The animation phase is advanced by the distance travelled, so the feet do not slide on the ground.
    /// </summary>
    public double WalkStrideLength { get; set; } = 0.36;

    /// <summary>
    /// Gets or sets the distance covered during one running gait cycle, as a fraction of the sprite size.
    /// </summary>
    public double RunStrideLength { get; set; } = 0.8;

    /// <summary>
    /// Gets or sets the distance to the cursor at which the animal starts to run instead of walk.
    /// </summary>
    public double RunDistance { get; set; } = 260;

    /// <summary>
    /// Gets or sets the minimum distance to the cursor at which the animal stops chasing it.
    /// The animal keeps at least one sprite size away so its window does not cover the cursor.
    /// </summary>
    public double CatchRadius { get; set; } = 48;

    /// <summary>
    /// Gets or sets how quickly the animal reaches the target speed, the higher the sharper the turns.
    /// </summary>
    public double Responsiveness { get; set; } = 6;

    /// <summary>
    /// Gets or sets the speed below which a decelerating animal is considered to be standing still.
    /// </summary>
    public double StopSpeed { get; set; } = 8;

    /// <summary>
    /// Gets or sets the horizontal speed required to change the direction the animal faces.
    /// </summary>
    public double FacingThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the cursor movement in pixels that is treated as an activity.
    /// </summary>
    public double CursorMoveThreshold { get; set; } = 3;

    /// <summary>
    /// Gets or sets the number of seconds of cursor inactivity before the animal falls asleep.
    /// </summary>
    public double SleepAfterSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the number of sleeping animations available to the animal. A random zero-based
    /// animation index is selected each time the animal falls asleep.
    /// </summary>
    public int SleepAnimationCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the minimum interval between blinks in seconds.
    /// </summary>
    public double BlinkMinIntervalSeconds { get; set; } = 2;

    /// <summary>
    /// Gets or sets the maximum interval between blinks in seconds.
    /// </summary>
    public double BlinkMaxIntervalSeconds { get; set; } = 6;

    /// <summary>
    /// Gets or sets the blink duration in seconds.
    /// </summary>
    public double BlinkDurationSeconds { get; set; } = 0.13;

    /// <summary>
    /// Gets or sets the tricks the animal can play while chasing is paused. Override this in an
    /// animal's options to give it a different set of common or animal-specific tricks.
    /// </summary>
    public IReadOnlyList<AnimalTrick> Tricks { get; set; } = DefaultTricks;

    /// <summary>
    /// Gets or sets the minimum interval between tricks in seconds. Tricks are played only while chasing
    /// is paused and the animal is awake, the time is not counted otherwise.
    /// </summary>
    public double TrickMinIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the maximum interval between tricks in seconds.
    /// </summary>
    public double TrickMaxIntervalSeconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets the minimum distance of the trick walk, as a fraction of the sprite size.
    /// </summary>
    public double TrickWalkMinDistance { get; set; } = 0.6;

    /// <summary>
    /// Gets or sets the maximum distance of the trick walk, as a fraction of the sprite size.
    /// </summary>
    public double TrickWalkMaxDistance { get; set; } = 1.5;

    /// <summary>
    /// Gets or sets the durations in seconds of the animal-specific tricks, such as
    /// <see cref="AnimalTrick.CapybaraSong" />. The common tricks have built-in durations.
    /// </summary>
    public IReadOnlyDictionary<AnimalTrick, double> TrickDurations { get; set; } =
        ReadOnlyDictionary<AnimalTrick, double>.Empty;

    /// <summary>
    /// Gets or sets the maximum processed time step in seconds, protects from jumps after a suspend.
    /// </summary>
    public double MaxDeltaSeconds { get; set; } = 0.1;
}
