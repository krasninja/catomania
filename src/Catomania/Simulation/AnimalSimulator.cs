using Catomania.Models;

namespace Catomania.Simulation;

/// <summary>
/// Simulates the animal: it walks and runs towards the cursor, stops nearby, idles and falls asleep
/// when the cursor does not move for a while. While chasing is paused it plays a random trick from time
/// to time: a short walk, looking around, a hop or its sound. The class is pure logic and knows nothing about
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

    private const double LookAroundGlanceSeconds = 0.6;
    private const int LookAroundGlances = 5;
    private const double HopDurationSeconds = 0.8;
    private const double SpeakDurationSeconds = 2;
    private const double TrickWalkRestSeconds = 0.6;
    private const int TrickWalkMinLegs = 2;
    private const int TrickWalkMaxLegs = 3;

    // Used for the trick walk distance when the sprite size is not known.
    private const double DefaultSpriteSize = 100;

    private static readonly AnimalTrick[] _tricks =
        [
            AnimalTrick.Walk,
            AnimalTrick.LookAround,
            AnimalTrick.Hop,
            AnimalTrick.Speak
        ];

    private readonly AnimalSimulatorOptions _options;
    private readonly Random _random;

    private ScreenPoint _cursorPosition;
    private double _cursorIdleTime;
    private double _blinkCooldown;
    private double _blinkTimeLeft;
    private double _trickCooldown;
    private double _trickTime;
    private bool _trickFacingRight;
    private TrickWalkStep _trickWalkStep;
    private int _trickWalkLegsLeft;
    private ScreenPoint _trickWalkOrigin;
    private ScreenPoint _trickWalkTarget;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimalSimulator" /> class.
    /// </summary>
    /// <param name="options">Simulation options, default options are used when not set.</param>
    /// <param name="random">Random generator used for blinks and tricks, pass a seeded one for deterministic tests.</param>
    public AnimalSimulator(AnimalSimulatorOptions? options = null, Random? random = null)
    {
        _options = options ?? new AnimalSimulatorOptions();
        _random = random ?? new Random();
        _blinkCooldown = GetNextBlinkInterval();
        _trickCooldown = GetNextTrickInterval();
    }

    private enum TrickWalkStep
    {
        Out,
        Rest,
        Back,
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
    /// Gets the trick that is playing.
    /// </summary>
    public AnimalTrick Trick { get; private set; }

    /// <summary>
    /// Gets the progress of the trick from 0 to 1, it is always 0 for the walk.
    /// </summary>
    public double TrickProgress { get; private set; }

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
            StopTrick();
            State = AnimalAnimationState.Dragged;
            Velocity = ScreenPoint.Zero;
            IsBlinking = false;
            _cursorPosition = input.CursorPosition;
            _cursorIdleTime = 0;
            Phase = WrapPhase(Phase + (deltaSeconds * DraggedPhaseRate));
            return;
        }

        TrackCursorActivity(input.CursorPosition, deltaSeconds);

        if (!input.IsPaused)
        {
            StopTrick();
        }
        else if (Trick != AnimalTrick.None || TryStartTrick(deltaSeconds, input))
        {
            UpdateTrick(deltaSeconds, input);
            return;
        }

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
        if (input.IsPaused && state == AnimalAnimationState.Idle)
        {
            FacingRight = input.CursorPosition.X >= Position.X;
        }
        else if (Math.Abs(Velocity.X) > _options.FacingThreshold)
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
    public AnimalRenderState CreateRenderState() => new(State, FacingRight, Phase, IsBlinking, Trick, TrickProgress);

    /// <summary>
    /// Start the trick at once.
    /// </summary>
    /// <param name="trick">Trick to play.</param>
    /// <param name="input">Simulation input.</param>
    public void StartTrick(AnimalTrick trick, in AnimalInput input)
    {
        if (trick == AnimalTrick.None)
        {
            StopTrick();
            return;
        }

        Trick = trick;
        TrickProgress = 0;
        _trickTime = 0;
        _trickFacingRight = FacingRight;
        if (trick == AnimalTrick.Walk)
        {
            _trickWalkStep = TrickWalkStep.Out;
            _trickWalkOrigin = Position;
            _trickWalkLegsLeft = _random.Next(TrickWalkMinLegs, TrickWalkMaxLegs + 1) - 1;
            _trickWalkTarget = GetTrickWalkTarget(input);
        }
    }

    private bool TryStartTrick(double deltaSeconds, in AnimalInput input)
    {
        // Only an awake animal standing still plays tricks.
        if (State != AnimalAnimationState.Idle || Velocity != ScreenPoint.Zero)
        {
            return false;
        }

        _trickCooldown -= deltaSeconds;
        if (_trickCooldown > 0)
        {
            return false;
        }

        StartTrick(_tricks[_random.Next(_tricks.Length)], input);
        return true;
    }

    private void StopTrick()
    {
        if (Trick == AnimalTrick.None)
        {
            return;
        }

        Trick = AnimalTrick.None;
        TrickProgress = 0;
        _trickCooldown = GetNextTrickInterval();
    }

    private void UpdateTrick(double deltaSeconds, in AnimalInput input)
    {
        _trickTime += deltaSeconds;
        var duration = Trick switch
        {
            AnimalTrick.LookAround => LookAroundGlanceSeconds * LookAroundGlances,
            AnimalTrick.Hop => HopDurationSeconds,
            AnimalTrick.Speak => SpeakDurationSeconds,
            _ => 0,
        };

        var state = AnimalAnimationState.Idle;
        var isFinished = false;
        var previousPosition = Position;
        if (Trick == AnimalTrick.Walk)
        {
            (state, isFinished) = UpdateTrickWalk(deltaSeconds, input);
        }
        else
        {
            Velocity = ScreenPoint.Zero;
            TrickProgress = Math.Min(1, _trickTime / duration);
            isFinished = _trickTime >= duration;
            if (Trick == AnimalTrick.LookAround)
            {
                // Looks away and back again on every other glance.
                var glance = Math.Min((int)(_trickTime / LookAroundGlanceSeconds), LookAroundGlances - 1);
                FacingRight = glance % 2 == 0 ? _trickFacingRight : !_trickFacingRight;
            }
            else
            {
                FacingRight = input.CursorPosition.X >= Position.X;
            }
        }

        State = state;
        Phase = WrapPhase(Phase + GetPhaseDelta(state, (Position - previousPosition).Length, deltaSeconds, input));
        UpdateBlink(deltaSeconds, state);
        if (isFinished)
        {
            StopTrick();
        }
    }

    private (AnimalAnimationState State, bool IsFinished) UpdateTrickWalk(double deltaSeconds, in AnimalInput input)
    {
        if (_trickWalkStep == TrickWalkStep.Rest)
        {
            if (_trickTime >= TrickWalkRestSeconds)
            {
                // Wanders a few legs, then walks back home.
                if (_trickWalkLegsLeft > 0)
                {
                    _trickWalkLegsLeft--;
                    _trickWalkTarget = GetTrickWalkTarget(input);
                    _trickWalkStep = TrickWalkStep.Out;
                }
                else
                {
                    _trickWalkStep = TrickWalkStep.Back;
                }
            }

            return (AnimalAnimationState.Idle, false);
        }

        var target = input.Bounds.Clamp(_trickWalkStep == TrickWalkStep.Out ? _trickWalkTarget : _trickWalkOrigin);
        var toTarget = target - Position;
        var distance = toTarget.Length;
        var walkSpeed = _options.WalkSpeed * Math.Max(0, input.SpeedMultiplier);
        if (distance < 1 || walkSpeed <= 0)
        {
            Position = target;
            Velocity = ScreenPoint.Zero;
            _trickTime = 0;
            if (_trickWalkStep == TrickWalkStep.Back)
            {
                return (AnimalAnimationState.Idle, true);
            }

            _trickWalkStep = TrickWalkStep.Rest;
            return (AnimalAnimationState.Idle, false);
        }

        var desired = toTarget.Normalized() * Math.Min(walkSpeed, distance / deltaSeconds);
        var smoothing = 1 - Math.Exp(-_options.Responsiveness * deltaSeconds);
        Velocity += (desired - Velocity) * smoothing;
        Position = input.Bounds.Clamp(Position + (Velocity * deltaSeconds));
        if (Math.Abs(Velocity.X) > _options.FacingThreshold)
        {
            FacingRight = Velocity.X > 0;
        }

        return (AnimalAnimationState.Walk, false);
    }

    private ScreenPoint GetTrickWalkTarget(in AnimalInput input)
    {
        var spriteSize = input.SpriteSize > 0 ? input.SpriteSize : DefaultSpriteSize;
        var range = _options.TrickWalkMaxDistance - _options.TrickWalkMinDistance;
        var distance = (_options.TrickWalkMinDistance + (_random.NextDouble() * Math.Max(0, range))) * spriteSize;
        var angle = _random.NextDouble() * TwoPi;
        var offset = new ScreenPoint(Math.Cos(angle) * distance, Math.Sin(angle) * distance);

        // Near the edge of the screen turn away from it, like a ball bouncing off a wall.
        var bounds = input.Bounds;
        var unclamped = Position + offset;
        if (unclamped.X < Math.Min(bounds.Left, bounds.Right) || unclamped.X > Math.Max(bounds.Left, bounds.Right))
        {
            offset = new ScreenPoint(-offset.X, offset.Y);
        }

        if (unclamped.Y < Math.Min(bounds.Top, bounds.Bottom) || unclamped.Y > Math.Max(bounds.Top, bounds.Bottom))
        {
            offset = new ScreenPoint(offset.X, -offset.Y);
        }

        var target = bounds.Clamp(Position + offset);
        return target;
    }

    private double GetNextTrickInterval()
    {
        var range = _options.TrickMaxIntervalSeconds - _options.TrickMinIntervalSeconds;
        return _options.TrickMinIntervalSeconds + (_random.NextDouble() * Math.Max(0, range));
    }

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
