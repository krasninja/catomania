using Catomania.Simulation;
using Xunit;

namespace Catomania.Tests.Simulation;

/// <summary>
/// Tests for <see cref="SimulationRect" /> and <see cref="ScreenPoint" />.
/// </summary>
public class SimulationRectTests
{
    [Fact]
    public void Contains_PointInside_ShouldReturnTrue()
    {
        // Arrange.
        var rect = new SimulationRect(10, 20, 100, 50);

        // Act & Assert.
        Assert.True(rect.Contains(new ScreenPoint(10, 20)));
        Assert.True(rect.Contains(new ScreenPoint(110, 70)));
        Assert.True(rect.Contains(new ScreenPoint(60, 45)));
        Assert.False(rect.Contains(new ScreenPoint(9, 45)));
        Assert.False(rect.Contains(new ScreenPoint(60, 71)));
    }

    [Fact]
    public void Clamp_PointOutside_ShouldReturnPointInside()
    {
        // Arrange.
        var rect = new SimulationRect(10, 20, 100, 50);

        // Act.
        var clamped = rect.Clamp(new ScreenPoint(-500, 4000));

        // Assert.
        Assert.Equal(new ScreenPoint(10, 70), clamped);
        Assert.True(rect.Contains(clamped));
    }

    [Fact]
    public void Clamp_EmptyRect_ShouldKeepThePoint()
    {
        // Act.
        var clamped = SimulationRect.Empty.Clamp(new ScreenPoint(15, 25));

        // Assert.
        Assert.Equal(new ScreenPoint(15, 25), clamped);
    }

    [Fact]
    public void Deflate_Rect_ShouldShrinkFromAllSides()
    {
        // Arrange.
        var rect = new SimulationRect(10, 10, 100, 100);

        // Act.
        var deflated = rect.Deflate(20);

        // Assert.
        Assert.Equal(new SimulationRect(30, 30, 60, 60), deflated);
    }

    [Fact]
    public void Union_TwoRects_ShouldReturnCombinedRect()
    {
        // Arrange.
        var left = new SimulationRect(0, 0, 100, 100);
        var right = new SimulationRect(200, 50, 100, 100);

        // Act.
        var union = SimulationRect.Union(left, right);

        // Assert.
        Assert.Equal(new SimulationRect(0, 0, 300, 150), union);
    }

    [Fact]
    public void Union_EmptyRect_ShouldReturnTheOtherOne()
    {
        // Arrange.
        var rect = new SimulationRect(5, 5, 10, 10);

        // Act & Assert.
        Assert.Equal(rect, SimulationRect.Union(SimulationRect.Empty, rect));
        Assert.Equal(rect, SimulationRect.Union(rect, SimulationRect.Empty));
    }

    [Fact]
    public void ScreenPoint_Normalized_ShouldReturnUnitVector()
    {
        // Act.
        var normalized = new ScreenPoint(30, 40).Normalized();

        // Assert.
        Assert.Equal(0.6, normalized.X, 10);
        Assert.Equal(0.8, normalized.Y, 10);
        Assert.Equal(1, normalized.Length, 10);
    }

    [Fact]
    public void ScreenPoint_ZeroVector_NormalizedReturnsZero()
    {
        // Act.
        var normalized = ScreenPoint.Zero.Normalized();

        // Assert.
        Assert.Equal(ScreenPoint.Zero, normalized);
    }
}
