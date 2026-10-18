using Catomania.Models;

namespace Catomania.Simulation;

/// <summary>
/// Simulates the cat: it walks and runs towards the cursor, stops nearby, idles and falls asleep
/// when the cursor does not move for a while. The class is pure logic and knows nothing about
/// windows and timers, so it can be unit tested.
/// </summary>
public sealed class CatSimulator
{
    private const double TwoPi = Math.PI * 2;
    private const double IdlePhaseRate = 2.2;
    private const double WalkPhaseRate = 9;
    private const double RunPhaseRate = 15;
    private const double SleepPhaseRate = 1.3;
    private const double DraggedPhaseRate = 3.5;

    private readonly CatSimulatorOptions _options;
    private readonly Random _random;

    private ScreenPoint _cursorPosition;
    private double _cursorIdleTime;
    private double _blinkCooldown;
    private double _blinkTimeLeft;

    /// <summary>
    /// Initializes a new instance of the <see cref="CatSimulator" /> class.
    /// </summary>
    /// <param name="options">Simulation options, default options are used when not set.</param>
    /// <param name="random">Random generator used for blinks, pass a seeded one for deterministic tests.</param>
    public CatSimulator(CatSimulatorOptions? options = null, Random? random = null)
    {
        _options = options ?? new CatSimulatorOptions();
        _random = random ?? new Random();
        _blinkCooldown = GetNextBlinkInterval();
    }

    /// <summary>
    /// Gets the simulation options.
    /// </summary>
    public CatSimulatorOptions Options => _options;

    /// <summary>
    /// Gets the cat center position in physical screen pixels.
    /// </summary>
    public ScreenPoint Position { get; private set; }

    /// <summary>
    /// Gets the current velocity in physical screen pixels per second.
    /// </summary>
    public ScreenPoint Velocity { get; private set; }

    /// <summary>
    /// Gets the current animation state.
    /// </summary>
    public CatAnimationState State { get; private set; } = CatAnimationState.Idle;

    /// <summary>
    /// Gets a value indicating whether the cat looks to the right.
    /// </summary>
    public bool FacingRight { get; private set; } = true;

    /// <summary>
    /// Gets the animation phase in radians.
    /// </summary>
    public double Phase { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the cat has closed its eyes for a blink.
    /// </summary>
    public bool IsBlinking { get; private set; }

    /// <summary>
    /// Gets the number of seconds the cursor has not been moving.
    /// </summary>
    public double CursorIdleTime => _cursorIdleTime;

    /// <summary>
    /// Move the cat to the position without any transition, keeps the velocity zero.
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
    public void Update(double deltaSeconds, in CatInput input)
    {
        if (deltaSeconds <= 0)
        {
            return;
        }

        deltaSeconds = Math.Min(deltaSeconds, _options.MaxDeltaSeconds);

        if (input.IsDragging)
        {
            State = CatAnimationState.Dragged;
            Velocity = ScreenPoint.Zero;
            IsBlinking = false;
            _cursorPosition = input.CursorPosition;
            _cursorIdleTime = 0;
            Phase = WrapPhase(Phase + (deltaSeconds * DraggedPhaseRate));
            return;
        }

        TrackCursorActivity(input.CursorPosition, deltaSeconds);

        var toCursor = input.CursorPosition - Position;
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

        Position += Velocity * deltaSeconds;
        Position = input.Bounds.Clamp(Position);
        if (Math.Abs(Velocity.X) > _options.FacingThreshold)
        {
            FacingRight = Velocity.X > 0;
        }

        var phaseRate = GetPhaseRate(state);
        if (state is CatAnimationState.Walk or CatAnimationState.Run)
        {
            phaseRate *= Math.Max(0.2, input.SpeedMultiplier);
        }

        State = state;
        Phase = WrapPhase(Phase + (deltaSeconds * phaseRate));
        UpdateBlink(deltaSeconds, state);
    }

    /// <summary>
    /// Create a render state snapshot for the current frame.
    /// </summary>
    /// <returns>Render state.</returns>
    public CatRenderState CreateRenderState() => new(State, FacingRight, Phase, IsBlinking);

    private (CatAnimationState State, double Speed) GetTargetMotion(double distance, in CatInput input)
    {
        if (input.IsPaused || distance <= _options.CatchRadius)
        {
            return _cursorIdleTime >= _options.SleepAfterSeconds
                ? (CatAnimationState.Sleep, 0)
                : (CatAnimationState.Idle, 0);
        }

        var isRunning = distance >= _options.RunDistance;
        var baseSpeed = isRunning ? _options.RunSpeed : _options.WalkSpeed;
        return isRunning
            ? (CatAnimationState.Run, baseSpeed * Math.Max(0, input.SpeedMultiplier))
            : (CatAnimationState.Walk, baseSpeed * Math.Max(0, input.SpeedMultiplier));
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

    private void UpdateBlink(double deltaSeconds, CatAnimationState state)
    {
        if (state is CatAnimationState.Sleep or CatAnimationState.Dragged)
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

    private static double GetPhaseRate(CatAnimationState state) => state switch
    {
        CatAnimationState.Walk => WalkPhaseRate,
        CatAnimationState.Run => RunPhaseRate,
        CatAnimationState.Sleep => SleepPhaseRate,
        CatAnimationState.Dragged => DraggedPhaseRate,
        _ => IdlePhaseRate,
    };

    private static double WrapPhase(double phase)
    {
        phase %= TwoPi;
        return phase < 0 ? phase + TwoPi : phase;
    }
}
