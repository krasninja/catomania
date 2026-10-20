using Catomania.Models;
using Catomania.Simulation;
using Xunit;

namespace Catomania.Tests.Simulation;

/// <summary>
/// Tests for <see cref="CatSimulator" />.
/// </summary>
public class CatSimulatorTests
{
    private const double Step = 1.0 / 60;

    private static readonly SimulationRect Bounds = new(0, 0, 2000, 1000);

    private static CatSimulator CreateSimulator(ScreenPoint position, CatSimulatorOptions? options = null)
    {
        var simulator = new CatSimulator(options, new Random(42));
        simulator.Teleport(position);
        return simulator;
    }

    private static CatInput CreateInput(
        ScreenPoint cursorPosition,
        SimulationRect? bounds = null,
        double speedMultiplier = 1,
        bool isDragging = false,
        bool isPaused = false)
        => new(cursorPosition, bounds ?? Bounds, speedMultiplier, isDragging, isPaused);

    private static void Run(CatSimulator simulator, CatInput input, double seconds)
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
        Assert.Equal(CatAnimationState.Run, simulator.State);
        Assert.True(simulator.Position.X > 520, $"the cat should run to the right, got {simulator.Position.X}");
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
        Assert.Equal(CatAnimationState.Walk, simulator.State);
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
        Assert.Equal(CatAnimationState.Idle, simulator.State);
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
        var simulator = CreateSimulator(new ScreenPoint(500, 500), new CatSimulatorOptions { SleepAfterSeconds = 2 });

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(510, 500)), 5);

        // Assert.
        Assert.Equal(CatAnimationState.Sleep, simulator.State);
        Assert.True(simulator.CursorIdleTime >= 2);
    }

    [Fact]
    public void Update_CursorMovesAgain_ShouldWakeUp()
    {
        // Arrange.
        var simulator = CreateSimulator(new ScreenPoint(500, 500), new CatSimulatorOptions { SleepAfterSeconds = 2 });
        Run(simulator, CreateInput(new ScreenPoint(510, 500)), 5);
        Assert.Equal(CatAnimationState.Sleep, simulator.State);

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(1500, 500)), 0.5);

        // Assert.
        Assert.Equal(CatAnimationState.Run, simulator.State);
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
        Assert.Equal(CatAnimationState.Dragged, simulator.State);
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
        Assert.Equal(CatAnimationState.Idle, simulator.State);
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

        // Assert. The cat comes to the edge and stops within the catch radius.
        Assert.InRange(simulator.Position.X, 600 - simulator.Options.CatchRadius, 600);
        Assert.Equal(300, simulator.Position.Y, 6);
    }

    [Fact]
    public void Update_CursorOutsideBounds_ShouldStopAtTheEdgeAndFallAsleep()
    {
        // Arrange. The cursor rests on the taskbar below the work area.
        var bounds = new SimulationRect(0, 0, 600, 600);
        var simulator = CreateSimulator(new ScreenPoint(300, 300), new CatSimulatorOptions { SleepAfterSeconds = 3 });

        // Act.
        Run(simulator, CreateInput(new ScreenPoint(300, 700), bounds), 5);

        // Assert.
        Assert.Equal(CatAnimationState.Sleep, simulator.State);
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
        Assert.Equal(CatAnimationState.Idle, simulator.State);
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
    }
}
