using Catomania.Models;

namespace Catomania.Simulation;

/// <summary>
/// Simulates the animal: it walks and runs towards the cursor, stops nearby, idles and falls asleep
/// when the cursor does not move for a while. The class is pure logic and knows nothing about
/// windows and timers, so it can be unit tested.
/// </summary>
public sealed class AnimalSimulator
{
    private const double TwoPi = Math.PI * 2;

    // The phase wraps every two cycles, so motions at half of the phase rate stay continuous.
    private const double PhasePeriod = TwoPi * 2;
    private const double IdlePhaseRate = 2.6;
    private const double WalkPhaseRate = 9;
    private const double RunPhaseRate = 15;
    private const double SleepPhaseRate = 1.3;
    private const double DraggedPhaseRate = 3.5;

    private readonly AnimalSimulatorOptions _options;
    private readonly Random _random;

    private ScreenPoint _cursorPosition;
    private double _cursorIdleTime;
    private double _blinkCooldown;
    private double _blinkTimeLeft;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimalSimulator" /> class.
    /// </summary>
    /// <param name="options">Simulation options, default options are used when not set.</param>
    /// <param name="random">Random generator used for blinks, pass a seeded one for deterministic tests.</param>
    public AnimalSimulator(AnimalSimulatorOptions? options = null, Random? random = null)
    {
        _options = options ?? new AnimalSimulatorOptions();
        _random = random ?? new Random();
        _blinkCooldown = GetNextBlinkInterval();
    }

    /// <summary>
    /// Gets the simulation options.
    /// </summary>
    public AnimalSimulatorOptions Options => _options;

    /// <summary>
    /// Gets the animal center position in physical screen pixels.
    /// </summary>
    public ScreenPoint Position { get; private set; }

    /// <summary>
    /// Gets the current velocity in physical screen pixels per second.
    /// </summary>
    public ScreenPoint Velocity { get; private set; }

    /// <summary>
    /// Gets the current animation state.
    /// </summary>
    public AnimalAnimationState State { get; private set; } = AnimalAnimationState.Idle;

    /// <summary>
    /// Gets a value indicating whether the animal looks to the right.
    /// </summary>
    public bool FacingRight { get; private set; } = true;

    /// <summary>
    /// Gets the animation phase in radians, wraps at 4π. While walking and running a full 2π cycle
    /// is one stride.
    /// </summary>
    public double Phase { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the animal has closed its eyes for a blink.
    /// </summary>
    public bool IsBlinking { get; private set; }

    /// <summary>
    /// Gets the number of seconds the cursor has not been moving.
    /// </summary>
    public double CursorIdleTime => _cursorIdleTime;

    /// <summary>
    /// Move the animal to the position without any transition, keeps the velocity zero.
    /// </summary>
    /// <param name="position">New position.</param>
    public void Teleport(ScreenPoint position)
    {
        Position = position;
        Velocity = ScreenPoint.Zero;
    }

    /// <summary>
    /// Advance the simulation.
    /// </summary>
    /// <param name="deltaSeconds">Elapsed time in seconds.</param>
    /// <param name="input">Simulation input.</param>
    public void Update(double deltaSeconds, in AnimalInput input)
    {
        if (deltaSeconds <= 0)
        {
            return;
        }

        deltaSeconds = Math.Min(deltaSeconds, _options.MaxDeltaSeconds);

        if (input.IsDragging)
        {
            State = AnimalAnimationState.Dragged;
            Velocity = ScreenPoint.Zero;
            IsBlinking = false;
            _cursorPosition = input.CursorPosition;
            _cursorIdleTime = 0;
            Phase = WrapPhase(Phase + (deltaSeconds * DraggedPhaseRate));
            return;
        }

        TrackCursorActivity(input.CursorPosition, deltaSeconds);

        // The cursor may be outside of the bounds (the taskbar, the screen edge), chase the closest
        // reachable point, otherwise the animal would keep walking in place at the edge.
        var target = input.Bounds.Clamp(input.CursorPosition);
        var toCursor = target - Position;
        var distance = toCursor.Length;
        var (state, targetSpeed) = GetTargetMotion(distance, input);

        // Do not move faster than needed to reach the cursor, it prevents jumping over it.
        var speed = Math.Min(targetSpeed, distance / deltaSeconds);
        var desired = distance < 1e-6 ? ScreenPoint.Zero : toCursor.Normalized() * speed;
        var smoothing = 1 - Math.Exp(-_options.Responsiveness * deltaSeconds);
        Velocity += (desired - Velocity) * smoothing;
        if (targetSpeed <= 0 && Velocity.Length < _options.StopSpeed)
        {
            Velocity = ScreenPoint.Zero;
        }

        var previousPosition = Position;
        Position += Velocity * deltaSeconds;
        Position = input.Bounds.Clamp(Position);
        if (Math.Abs(Velocity.X) > _options.FacingThreshold)
        {
            FacingRight = Velocity.X > 0;
        }

        State = state;
        Phase = WrapPhase(Phase + GetPhaseDelta(state, (Position - previousPosition).Length, deltaSeconds, input));
        UpdateBlink(deltaSeconds, state);
    }

    /// <summary>
    /// Create a render state snapshot for the current frame.
    /// </summary>
    /// <returns>Render state.</returns>
    public AnimalRenderState CreateRenderState() => new(State, FacingRight, Phase, IsBlinking);

    private (AnimalAnimationState State, double Speed) GetTargetMotion(double distance, in AnimalInput input)
    {
        if (input.IsPaused || distance <= _options.CatchRadius)
        {
            return _cursorIdleTime >= _options.SleepAfterSeconds
                ? (AnimalAnimationState.Sleep, 0)
                : (AnimalAnimationState.Idle, 0);
        }

        var isRunning = distance >= _options.RunDistance;
        var baseSpeed = isRunning ? _options.RunSpeed : _options.WalkSpeed;
        return isRunning
            ? (AnimalAnimationState.Run, baseSpeed * Math.Max(0, input.SpeedMultiplier))
            : (AnimalAnimationState.Walk, baseSpeed * Math.Max(0, input.SpeedMultiplier));
    }

    private void TrackCursorActivity(ScreenPoint cursorPosition, double deltaSeconds)
    {
        if (ScreenPoint.Distance(cursorPosition, _cursorPosition) > _options.CursorMoveThreshold)
        {
            _cursorPosition = cursorPosition;
            _cursorIdleTime = 0;
        }
        else
        {
            _cursorIdleTime += deltaSeconds;
        }
    }

    private void UpdateBlink(double deltaSeconds, AnimalAnimationState state)
    {
        if (state is AnimalAnimationState.Sleep or AnimalAnimationState.Dragged)
        {
            IsBlinking = false;
            return;
        }

        if (_blinkTimeLeft > 0)
        {
            _blinkTimeLeft = Math.Max(0, _blinkTimeLeft - deltaSeconds);
            IsBlinking = _blinkTimeLeft > 0;
            if (!IsBlinking)
            {
                _blinkCooldown = GetNextBlinkInterval();
            }

            return;
        }

        _blinkCooldown -= deltaSeconds;
        if (_blinkCooldown <= 0)
        {
            _blinkTimeLeft = _options.BlinkDurationSeconds;
            IsBlinking = true;
        }
    }

    private double GetNextBlinkInterval()
    {
        var range = _options.BlinkMaxIntervalSeconds - _options.BlinkMinIntervalSeconds;
        return _options.BlinkMinIntervalSeconds + (_random.NextDouble() * Math.Max(0, range));
    }

    private double GetPhaseDelta(
        AnimalAnimationState state,
        double distance,
        double deltaSeconds,
        in AnimalInput input)
    {
        if (state is not (AnimalAnimationState.Walk or AnimalAnimationState.Run))
        {
            return deltaSeconds * GetPhaseRate(state);
        }

        var strideLength = state == AnimalAnimationState.Run ? _options.RunStrideLength : _options.WalkStrideLength;
        var stride = strideLength * input.SpriteSize;
        if (stride > 0)
        {
            // One cycle per stride, a planted foot then moves back exactly as fast as the animal moves forward.
            return distance / stride * TwoPi;
        }

        return deltaSeconds * GetPhaseRate(state) * Math.Max(0.2, input.SpeedMultiplier);
    }

    private static double GetPhaseRate(AnimalAnimationState state) => state switch
    {
        AnimalAnimationState.Walk => WalkPhaseRate,
        AnimalAnimationState.Run => RunPhaseRate,
        AnimalAnimationState.Sleep => SleepPhaseRate,
        AnimalAnimationState.Dragged => DraggedPhaseRate,
        _ => IdlePhaseRate,
    };

    private static double WrapPhase(double phase)
    {
        phase %= PhasePeriod;
        return phase < 0 ? phase + PhasePeriod : phase;
    }
}
