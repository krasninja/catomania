namespace Catomania.Simulation;

/// <summary>
/// Tunable parameters of <see cref="AnimalSimulator" />. All speeds are in physical pixels per second.
/// </summary>
public sealed class AnimalSimulatorOptions
{
    /// <summary>
    /// Gets or sets the walking speed.
    /// </summary>
    public double WalkSpeed { get; set; } = 120;

    /// <summary>
    /// Gets or sets the running speed.
    /// </summary>
    public double RunSpeed { get; set; } = 430;

    /// <summary>
    /// Gets or sets the distance to the cursor at which the animal starts to run instead of walk.
    /// </summary>
    public double RunDistance { get; set; } = 260;

    /// <summary>
    /// Gets or sets the distance to the cursor at which the animal stops chasing it.
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
    /// Gets or sets the maximum processed time step in seconds, protects from jumps after a suspend.
    /// </summary>
    public double MaxDeltaSeconds { get; set; } = 0.1;
}
