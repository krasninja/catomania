using Catomania.Models;
using Catomania.Simulation;
using Xunit;

namespace Catomania.Tests.Simulation;

/// <summary>
/// Tests for <see cref="AnimalSimulator" />.
/// </summary>
public class AnimalSimulatorTests
{
    private const double Step = 1.0 / 60;

    private static readonly SimulationRect Bounds = new(0, 0, 2000, 1000);

    private static AnimalSimulator CreateSimulator(ScreenPoint position, AnimalSimulatorOptions? options = null)
    {
        var simulator = new AnimalSimulator(options, new Random(42));
        simulator.Teleport(position);
        return simulator;
    }

    private static AnimalInput CreateInput(
        ScreenPoint cursorPosition,
        SimulationRect? bounds = null,
        double speedMultiplier = 1,
        bool isDragging = false,
        bool isPaused = false)
        => new(cursorPosition, bounds ?? Bounds, speedMultiplier, isDragging, isPaused);

    private static AnimalSimulatorOptions CreateTrickOptions() => new()
    {
        TrickMinIntervalSeconds = 1,
        TrickMaxIntervalSeconds = 1,
    };

    private static void Run(AnimalSimulator simulator, AnimalInput input, double seconds)
    {
        for (var elapsed = 0.0; elapsed < seconds; elapsed += Step)
        {
            simulator.Update(Step, input);
        }
    }

    [Fact]
    public void Update_CursorIsFarAway_ShouldRunTowardsTheCursor()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500));

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(1500, 500)), 0.2);

        // Assert.
        Assert.Equal(AnimalAnimationState.Run, simulator.State);
        Assert.True(simulator.Position.X > 520, $"the animal should run to the right, got {simulator.Position.X}");
        Assert.True(simulator.Velocity.X > 0);
    }

    [Fact]
    public void Update_CursorIsNearby_ShouldWalkTowardsTheCursor()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500));

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(600, 500)), 0.2);

        // Assert.
        Assert.Equal(AnimalAnimationState.Walk, simulator.State);
        Assert.InRange(simulator.Position.X, 501, 599);
    }

    [Fact]
    public void Update_CursorWithinCatchRadius_ShouldStopAndIdle()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500));

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(520, 500)), 2);

        // Assert.
        Assert.Equal(AnimalAnimationState.Idle, simulator.State);
        Assert.Equal(ScreenPoint.Zero, simulator.Velocity);
        Assert.InRange(simulator.Position.X, 499, 521);
    }

    [Fact]
    public void Update_CursorOnTheLeft_ShouldFaceLeft()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500));

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(100, 500)), 0.3);

        // Assert.
        Assert.False(simulator.FacingRight);
        Assert.True(simulator.Position.X < 500);
    }

    [Fact]
    public void Update_CursorDoesNotMove_ShouldFallAsleep()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500), new AnimalSimulatorOptions { SleepAfterSeconds = 2 });

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(510, 500)), 5);

        // Assert.
        Assert.Equal(AnimalAnimationState.Sleep, simulator.State);
        Assert.True(simulator.CursorIdleTime >= 2);
    }

    [Fact]
    public void Update_CursorMovesAgain_ShouldWakeUp()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500), new AnimalSimulatorOptions { SleepAfterSeconds = 2 });
        Run(simulator, CreateInput(new ScreenPoint(510, 500)), 5);
        Assert.Equal(AnimalAnimationState.Sleep, simulator.State);

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(1500, 500)), 0.5);

        // Assert.
        Assert.Equal(AnimalAnimationState.Run, simulator.State);
        Assert.True(simulator.Position.X > 500);
    }

    [Fact]
    public void Update_Dragging_ShouldNotMoveAndStayDragged()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500));

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(1500, 500), isDragging: true), 0.5);

        // Assert.
        Assert.Equal(AnimalAnimationState.Dragged, simulator.State);
        Assert.Equal(new ScreenPoint(500, 500), simulator.Position);
        Assert.Equal(ScreenPoint.Zero, simulator.Velocity);
    }

    [Fact]
    public void Update_Paused_ShouldNotMove()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500));

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(1500, 500), isPaused: true), 1);

        // Assert.
        Assert.Equal(AnimalAnimationState.Idle, simulator.State);
        Assert.Equal(new ScreenPoint(500, 500), simulator.Position);
    }

    [Fact]
    public void Update_CursorOutsideBounds_ShouldStayInsideBounds()
    {
        // Arrange.
        var bounds = new SimulationRect(0, 0, 600, 600);
        var simulator = CreateSimulator(new ScreenPoint(300, 300));

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(5000, 300), bounds), 5);

        // Assert. The animal comes to the edge and stops within the catch radius.
        Assert.InRange(simulator.Position.X, 600 - simulator.Options.CatchRadius, 600);
        Assert.Equal(300, simulator.Position.Y, 6);
    }

    [Fact]
    public void Update_CursorOutsideBounds_ShouldStopAtTheEdgeAndFallAsleep()
    {
        // Arrange. The cursor rests on the taskbar below the work area.
        var bounds = new SimulationRect(0, 0, 600, 600);
        var simulator = CreateSimulator(new ScreenPoint(300, 300), new AnimalSimulatorOptions { SleepAfterSeconds = 3 });

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(300, 700), bounds), 5);

        // Assert.
        Assert.Equal(AnimalAnimationState.Sleep, simulator.State);
        Assert.Equal(ScreenPoint.Zero, simulator.Velocity);
    }

    [Fact]
    public void Update_FasterSpeed_ShouldMoveFurther()
    {
        // Arrange.
        var slowCat = CreateSimulator(new ScreenPoint(500, 500));
        var fastCat = CreateSimulator(new ScreenPoint(500, 500));
        var input = CreateInput(new ScreenPoint(1800, 500));

        // Act.
        Run(slowCat, input with { SpeedMultiplier = 0.5 }, 0.4);
        Run(fastCat, input with { SpeedMultiplier = 2 }, 0.4);

        // Assert.
        Assert.True(fastCat.Position.X > slowCat.Position.X);
    }

    [Fact]
    public void Update_ZeroDelta_ShouldNotChangeAnything()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500));

        // Act.
        simulator.Update(0, CreateInput(new ScreenPoint(1500, 500)));

        // Assert.
        Assert.Equal(new ScreenPoint(500, 500), simulator.Position);
        Assert.Equal(AnimalAnimationState.Idle, simulator.State);
        Assert.Equal(0, simulator.Phase);
    }

    [Fact]
    public void Update_Walking_ShouldAdvanceTheAnimationPhase()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500));

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(600, 500)), 0.3);

        // Assert.
        Assert.InRange(simulator.Phase, 0.0001, Math.PI * 2);
    }

    [Fact]
    public void Update_Idling_ShouldBlink()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500));
        var input = CreateInput(new ScreenPoint(510, 500));
        var hasBlinked = false;

        // Act.
        for (var elapsed = 0.0; elapsed < 10; elapsed += Step)
        {
            simulator.Update(Step, input);
            hasBlinked |= simulator.IsBlinking;
        }

        // Assert.
        Assert.True(hasBlinked);
    }

    [Fact]
    public void CreateRenderState_AnyState_ShouldReturnTheCurrentValues()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500));
        Run(simulator, CreateInput(new ScreenPoint(1500, 500)), 0.3);

        // Act.
        var state = simulator.CreateRenderState();

        // Assert.
        Assert.Equal(simulator.State, state.State);
        Assert.Equal(simulator.FacingRight, state.FacingRight);
        Assert.Equal(simulator.Phase, state.Phase);
        Assert.Equal(simulator.IsBlinking, state.IsBlinking);
        Assert.Equal(simulator.Trick, state.Trick);
        Assert.Equal(simulator.TrickProgress, state.TrickProgress);
    }

    [Fact]
    public void Update_PausedAndIdle_ShouldStartATrick()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500), CreateTrickOptions());
        var input = CreateInput(new ScreenPoint(510, 500), isPaused: true);
        var hasPlayedTrick = false;

        // Act.
        for (var elapsed = 0.0; elapsed < 2; elapsed += Step)
        {
            simulator.Update(Step, input);
            hasPlayedTrick |= simulator.Trick != AnimalTrick.None;
        }

        // Assert.
        Assert.True(hasPlayedTrick);
    }

    [Fact]
    public void Update_NotPaused_ShouldNotStartATrick()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500), CreateTrickOptions());
        var input = CreateInput(new ScreenPoint(510, 500));

        // Act.
        for (var elapsed = 0.0; elapsed < 5; elapsed += Step)
        {
            simulator.Update(Step, input);

            // Assert.
            Assert.Equal(AnimalTrick.None, simulator.Trick);
        }
    }

    [Fact]
    public void Update_PausedAndAsleep_ShouldNotStartATrick()
    {
        // Arrange.
        var options = CreateTrickOptions();
        options.SleepAfterSeconds = 0.5;
        var simulator = CreateSimulator(new ScreenPoint(500, 500), options);
        var input = CreateInput(new ScreenPoint(510, 500), isPaused: true);
        Run(simulator, input, 0.6);

        // Act.
        Run(simulator, input, 5);

        // Assert.
        Assert.Equal(AnimalAnimationState.Sleep, simulator.State);
        Assert.Equal(AnimalTrick.None, simulator.Trick);
    }

    [Fact]
    public void Update_TrickIsPlayingAndUnpaused_ShouldStopTheTrick()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500));
        simulator.StartTrick(AnimalTrick.LookAround, CreateInput(new ScreenPoint(510, 500), isPaused: true));

        // Act.
        simulator.Update(Step, CreateInput(new ScreenPoint(510, 500)));

        // Assert.
        Assert.Equal(AnimalTrick.None, simulator.Trick);
    }

    [Fact]
    public void Update_WalkTrick_ShouldWalkAwayAndComeBack()
    {
        // Arrange.
        var origin = new ScreenPoint(500, 500);
        var simulator = CreateSimulator(origin);
        var input = CreateInput(new ScreenPoint(510, 500), isPaused: true) with { SpriteSize = 100 };
        simulator.StartTrick(AnimalTrick.Walk, input);
        var maxDistance = 0.0;
        var hasWalked = false;

        // Act.
        for (var elapsed = 0.0; elapsed < 20 && simulator.Trick != AnimalTrick.None; elapsed += Step)
        {
            simulator.Update(Step, input);
            maxDistance = Math.Max(maxDistance, ScreenPoint.Distance(simulator.Position, origin));
            hasWalked |= simulator.State == AnimalAnimationState.Walk;
        }

        // Assert.
        Assert.True(hasWalked);
        Assert.True(maxDistance > 30, $"should walk away, got {maxDistance}");
        Assert.Equal(AnimalTrick.None, simulator.Trick);
        Assert.True(ScreenPoint.Distance(simulator.Position, origin) < 1, $"should come back, got {simulator.Position}");
    }

    [Fact]
    public void Update_WalkTrick_ShouldWalkBothHorizontallyAndVertically()
    {
        // Arrange.
        var origin = new ScreenPoint(500, 500);
        var simulator = CreateSimulator(origin);
        var input = CreateInput(new ScreenPoint(510, 500), isPaused: true) with { SpriteSize = 100 };
        var maxOffsetX = 0.0;
        var maxOffsetY = 0.0;

        // Act.
        for (var i = 0; i < 5; i++)
        {
            simulator.StartTrick(AnimalTrick.Walk, input);
            for (var elapsed = 0.0; elapsed < 20 && simulator.Trick != AnimalTrick.None; elapsed += Step)
            {
                simulator.Update(Step, input);
                maxOffsetX = Math.Max(maxOffsetX, Math.Abs(simulator.Position.X - origin.X));
                maxOffsetY = Math.Max(maxOffsetY, Math.Abs(simulator.Position.Y - origin.Y));
            }
        }

        // Assert.
        Assert.True(maxOffsetX > 30, $"should walk horizontally, got {maxOffsetX}");
        Assert.True(maxOffsetY > 30, $"should walk vertically, got {maxOffsetY}");
    }

    [Fact]
    public void Update_WalkTrickInTheCorner_ShouldWalkAwayFromTheEdges()
    {
        // Arrange.
        var origin = new ScreenPoint(Bounds.Right, Bounds.Bottom);
        var simulator = CreateSimulator(origin);
        var input = CreateInput(origin, isPaused: true) with { SpriteSize = 100 };
        simulator.StartTrick(AnimalTrick.Walk, input);

        // Act.
        Run(simulator, input, 1);

        // Assert.
        var distance = ScreenPoint.Distance(simulator.Position, origin);
        Assert.True(distance > 30, $"should walk out of the corner, got {distance}");
    }

    [Fact]
    public void Update_LookAroundTrick_ShouldTurnFromSideToSide()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500));
        var input = CreateInput(new ScreenPoint(510, 500), isPaused: true);
        simulator.Update(Step, input);
        simulator.StartTrick(AnimalTrick.LookAround, input);
        var hasLookedLeft = false;

        // Act.
        for (var elapsed = 0.0; elapsed < 5 && simulator.Trick != AnimalTrick.None; elapsed += Step)
        {
            simulator.Update(Step, input);
            hasLookedLeft |= !simulator.FacingRight;
        }

        // Assert.
        Assert.True(hasLookedLeft);
        Assert.True(simulator.FacingRight);
        Assert.Equal(AnimalTrick.None, simulator.Trick);
    }

    [Fact]
    public void Update_HopTrick_ShouldProgressAndFinishInPlace()
    {
        // Arrange.
        var position = new ScreenPoint(500, 500);
        var simulator = CreateSimulator(position);
        var input = CreateInput(new ScreenPoint(510, 500), isPaused: true);
        simulator.StartTrick(AnimalTrick.Hop, input);

        // Act.
        Run(simulator, input, 0.4);
        var progress = simulator.TrickProgress;
        Run(simulator, input, 1);

        // Assert.
        Assert.InRange(progress, 0.3, 0.7);
        Assert.Equal(AnimalTrick.None, simulator.Trick);
        Assert.Equal(position, simulator.Position);
    }

    [Fact]
    public void Update_ZoomiesTrick_ShouldRunAwayAndComeBack()
    {
        // Arrange.
        var origin = new ScreenPoint(1000, 500);
        var simulator = CreateSimulator(origin);
        var input = CreateInput(new ScreenPoint(1010, 500), isPaused: true) with { SpriteSize = 100 };
        simulator.StartTrick(AnimalTrick.Zoomies, input);
        var maxDistance = 0.0;
        var hasRun = false;

        // Act.
        for (var elapsed = 0.0; elapsed < 20 && simulator.Trick != AnimalTrick.None; elapsed += Step)
        {
            simulator.Update(Step, input);
            maxDistance = Math.Max(maxDistance, ScreenPoint.Distance(simulator.Position, origin));
            hasRun |= simulator.State == AnimalAnimationState.Run;
        }

        // Assert.
        Assert.True(hasRun);
        Assert.True(maxDistance > 150, $"should dash far, got {maxDistance}");
        Assert.Equal(AnimalTrick.None, simulator.Trick);
        Assert.True(ScreenPoint.Distance(simulator.Position, origin) < 1, $"should come back, got {simulator.Position}");
    }

    [Fact]
    public void Update_BunnyHopsTrick_ShouldHopAwayAndBack()
    {
        // Arrange.
        var origin = new ScreenPoint(500, 500);
        var simulator = CreateSimulator(origin);
        var input = CreateInput(new ScreenPoint(510, 500), isPaused: true) with { SpriteSize = 100 };
        simulator.StartTrick(AnimalTrick.BunnyHops, input);
        var maxDistance = 0.0;

        // Act.
        for (var elapsed = 0.0; elapsed < 10 && simulator.Trick != AnimalTrick.None; elapsed += Step)
        {
            simulator.Update(Step, input);
            maxDistance = Math.Max(maxDistance, ScreenPoint.Distance(simulator.Position, origin));
        }

        // Assert.
        Assert.True(maxDistance > 30, $"should hop away, got {maxDistance}");
        Assert.Equal(AnimalTrick.None, simulator.Trick);
        Assert.True(ScreenPoint.Distance(simulator.Position, origin) < 1, $"should come back, got {simulator.Position}");
    }

    [Theory]
    [InlineData(AnimalTrick.Emote)]
    [InlineData(AnimalTrick.Wiggle)]
    [InlineData(AnimalTrick.Shake)]
    [InlineData(AnimalTrick.Spin)]
    public void Update_TrickInPlace_ShouldFinishWhereItStarted(AnimalTrick trick)
    {
        // Arrange.
        var position = new ScreenPoint(500, 500);
        var simulator = CreateSimulator(position);
        var input = CreateInput(new ScreenPoint(510, 500), isPaused: true);
        simulator.StartTrick(trick, input);

        // Act.
        Run(simulator, input, 3);

        // Assert.
        Assert.Equal(AnimalTrick.None, simulator.Trick);
        Assert.Equal(position, simulator.Position);
    }
}
