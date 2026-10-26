using Catomania.Abstractions;
using Catomania.Models;
using Catomania.Services;
using Catomania.Simulation;
using Xunit;

namespace Catomania.Tests.Services;

/// <summary>
/// Tests for <see cref="AnimalChaseService" />.
/// </summary>
public class AnimalChaseServiceTests
{
    [Fact]
    public void Tick_CursorIsFarAway_ShouldMoveTheWindowTowardsTheCursor()
    {
        // Arrange.
        var host = new FakeAnimalHost();
        var service = CreateService(host, new StubCursorPositionProvider(new ScreenPoint(1500, 500)));

        // Act.
        for (var i = 0; i < 30; i++)
        {
            service.Tick(1.0 / 60);
        }

        // Assert.
        Assert.True(host.Center.X > 500, $"the window should follow the cursor, got {host.Center.X}");
        Assert.True(host.SetCenterCalls > 0);
    }

    [Fact]
    public void Tick_WhenDragging_ShouldFollowTheWindow()
    {
        // Arrange.
        var host = new FakeAnimalHost { IsDragging = true };
        var service = CreateService(host, new StubCursorPositionProvider(new ScreenPoint(1500, 500)));

        // Act.
        host.Center = new ScreenPoint(900, 700);
        service.Tick(1.0 / 60);

        // Assert.
        Assert.Equal(0, host.SetCenterCalls);
        Assert.Equal(new ScreenPoint(900, 700), service.Simulator.Position);
        Assert.Equal(AnimalAnimationState.Dragged, host.LastRenderState.State);
    }

    [Fact]
    public void Tick_ShouldApplyTheRenderState()
    {
        // Arrange.
        var host = new FakeAnimalHost();
        var service = CreateService(host, new StubCursorPositionProvider(new ScreenPoint(1500, 500)));

        // Act.
        service.Tick(0.5);

        // Assert.
        Assert.Equal(AnimalAnimationState.Run, host.LastRenderState.State);
        Assert.True(host.LastRenderState.FacingRight);
    }

    [Fact]
    public void Tick_CursorPositionIsNotAvailable_ShouldKeepTheWindowInPlace()
    {
        // Arrange.
        var host = new FakeAnimalHost();
        var provider = new StubCursorPositionProvider(new ScreenPoint(1500, 500)) { Available = false };
        var service = CreateService(host, provider);

        // Act.
        for (var i = 0; i < 30; i++)
        {
            service.Tick(1.0 / 60);
        }

        // Assert.
        Assert.Equal(new ScreenPoint(500, 500), host.Center);
        Assert.Equal(AnimalAnimationState.Idle, host.LastRenderState.State);
    }

    [Fact]
    public void Tick_Paused_ShouldNotMoveTheWindow()
    {
        // Arrange.
        var settings = new AppSettings { Paused = true };
        var host = new FakeAnimalHost();
        var service = CreateService(host, new StubCursorPositionProvider(new ScreenPoint(1500, 500)), settings);

        // Act.
        for (var i = 0; i < 30; i++)
        {
            service.Tick(1.0 / 60);
        }

        // Assert.
        Assert.Equal(new ScreenPoint(500, 500), host.Center);
    }

    [Fact]
    public void Recenter_ShouldPutTheCatNextToTheCursor()
    {
        // Arrange.
        var host = new FakeAnimalHost();
        var service = CreateService(host, new StubCursorPositionProvider(new ScreenPoint(800, 400)));

        // Act.
        service.Recenter();
        service.Tick(1.0 / 60);

        // Assert.
        Assert.True(host.Center.Y > 400, "the animal should spawn below the cursor");
        Assert.InRange(host.Center.X, 799, 801);
    }

    [Fact]
    public void Tick_CursorOnTheSecondScreen_ShouldUseItsWorkAreaAsBounds()
    {
        // Arrange.
        var host = new FakeAnimalHost
        {
            WorkAreas =
            [
                new SimulationRect(0, 0, 1000, 1000),
                new SimulationRect(1000, 0, 1000, 1000),
            ],
            Center = new ScreenPoint(1500, 500),
        };
        var service = CreateService(host, new StubCursorPositionProvider(new ScreenPoint(1600, 500)));

        // Act.
        service.Tick(1.0 / 60);

        // Assert. The second screen work area deflated by half of the sprite size.
        Assert.Equal(new SimulationRect(1050, 50, 900, 900), service.Bounds);
        Assert.True(host.Center.X >= 1500);
    }

    [Fact]
    public void Tick_CursorMovesToAnotherScreen_ShouldStayOnItsScreen()
    {
        // Arrange.
        var host = new FakeAnimalHost
        {
            WorkAreas =
            [
                new SimulationRect(0, 0, 1000, 1000),
                new SimulationRect(1000, 0, 1000, 1000),
            ],
            Center = new ScreenPoint(200, 500),
        };
        var service = CreateService(host, new StubCursorPositionProvider(new ScreenPoint(1800, 500)));

        // Act.
        service.Tick(1.0 / 60);

        // Assert. The first screen work area deflated by half of the sprite size.
        Assert.Equal(new SimulationRect(50, 50, 900, 900), service.Bounds);
        Assert.InRange(host.Center.X, 200, 220);

        // Act. The animal reaches the screen edge and stops there.
        for (var i = 0; i < 600; i++)
        {
            service.Tick(1.0 / 60);
        }

        // Assert.
        Assert.InRange(host.Center.X, 200, 950);
    }

    [Fact]
    public void Recenter_BeforeStart_ShouldKeepTheCatInsideTheWorkArea()
    {
        // Arrange. The cursor is at the bottom of the screen, the animal spawns below it.
        var host = new FakeAnimalHost();
        var service = CreateService(host, new StubCursorPositionProvider(new ScreenPoint(800, 990)));

        // Act.
        service.Recenter();

        // Assert.
        Assert.Equal(950, service.Simulator.Position.Y, 6);
    }

    /// <summary>
    /// Create the service the same way the application does: the animal starts where the window is.
    /// </summary>
    private static AnimalChaseService CreateService(
        FakeAnimalHost host,
        ICursorPositionProvider cursorPositionProvider,
        AppSettings? settings = null)
    {
        var service = new AnimalChaseService(
            host,
            cursorPositionProvider,
            new AnimalSimulator(random: new Random(42)),
            settings ?? new AppSettings());
        service.Simulator.Teleport(host.GetCenter());
        return service;
    }

    private sealed class FakeAnimalHost : IAnimalHost
    {
        public double Scaling { get; set; } = 1;

        public bool IsDragging { get; set; }

        public ScreenPoint SpriteSize { get; set; } = new(100, 100);

        public ScreenPoint Center { get; set; } = new(500, 500);

        public IReadOnlyList<SimulationRect> WorkAreas { get; set; } = [new SimulationRect(0, 0, 2000, 1000)];

        public AnimalRenderState LastRenderState { get; private set; }

        public int SetCenterCalls { get; private set; }

        public ScreenPoint GetCenter() => Center;

        public void SetCenter(ScreenPoint center)
        {
            Center = center;
            SetCenterCalls++;
        }

        public IReadOnlyList<SimulationRect> GetWorkAreas() => WorkAreas;

        public void ApplyRenderState(AnimalRenderState state) => LastRenderState = state;
    }

    private sealed class StubCursorPositionProvider : ICursorPositionProvider
    {
        public StubCursorPositionProvider(ScreenPoint position)
        {
            Position = position;
        }

        public ScreenPoint Position { get; set; }

        public bool Available { get; set; } = true;

        public string Name => "Stub";

        public bool IsAvailable => Available;

        public bool TryGetPosition(out ScreenPoint position)
        {
            position = Position;
            return Available;
        }
    }
}
