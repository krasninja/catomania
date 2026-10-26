using Catomania.Abstractions;
using Catomania.Input;
using Catomania.Simulation;
using Xunit;

namespace Catomania.Tests.Input;

/// <summary>
/// Tests for <see cref="CompositeCursorPositionProvider" /> and <see cref="FallbackCursorPositionProvider" />.
/// </summary>
public class CompositeCursorPositionProviderTests
{
    [Fact]
    public void TryGetPosition_FirstProviderIsNotAvailable_ShouldUseTheSecondOne()
    {
        // Arrange.
        var unavailable = new StubCursorPositionProvider("Native") { Available = false };
        var fallback = new StubCursorPositionProvider("Fallback") { Position = new ScreenPoint(10, 20) };
        var provider = new CompositeCursorPositionProvider([unavailable, fallback]);

        // Act.
        var result = provider.TryGetPosition(out var position);

        // Assert.
        Assert.True(result);
        Assert.Equal(new ScreenPoint(10, 20), position);
        Assert.Equal("Fallback", provider.ActiveProviderName);
        Assert.Equal(0, unavailable.Calls);
    }

    [Fact]
    public void TryGetPosition_FirstProviderFails_ShouldUseTheSecondOne()
    {
        // Arrange.
        var failing = new StubCursorPositionProvider("Native") { Succeeds = false };
        var fallback = new StubCursorPositionProvider("Fallback") { Position = new ScreenPoint(30, 40) };
        var provider = new CompositeCursorPositionProvider([failing, fallback]);

        // Act.
        var result = provider.TryGetPosition(out var position);

        // Assert.
        Assert.True(result);
        Assert.Equal(new ScreenPoint(30, 40), position);
        Assert.Equal(1, failing.Calls);
    }

    [Fact]
    public void TryGetPosition_AllProvidersFail_ShouldReturnFalse()
    {
        // Arrange.
        var provider = new CompositeCursorPositionProvider(
        [
            new StubCursorPositionProvider("First") { Succeeds = false },
            new StubCursorPositionProvider("Second") { Succeeds = false },
        ]);

        // Act.
        var result = provider.TryGetPosition(out var position);

        // Assert.
        Assert.False(result);
        Assert.Equal(ScreenPoint.Zero, position);
        Assert.Null(provider.ActiveProviderName);
    }

    [Fact]
    public void FallbackCursorPositionProvider_NothingReported_ShouldReturnFalse()
    {
        // Arrange.
        var provider = new FallbackCursorPositionProvider();

        // Act.
        var firstResult = provider.TryGetPosition(out _);
        provider.Report(new ScreenPoint(5, 6));
        var secondResult = provider.TryGetPosition(out var position);

        // Assert.
        Assert.False(firstResult);
        Assert.True(secondResult);
        Assert.Equal(new ScreenPoint(5, 6), position);
    }

    [Fact]
    public void CreateDefault_ShouldRegisterTheFallbackProvider()
    {
        // Arrange.
        var fallback = new FallbackCursorPositionProvider();

        // Act.
        using var provider = CursorPositionProviderFactory.CreateDefault(fallback);
        fallback.Report(new ScreenPoint(11, 22));
        var result = provider.TryGetPosition(out var position);

        // Assert. The native provider is used when it works, otherwise the reported one.
        Assert.True(provider.IsAvailable);
        Assert.Contains("Window", provider.Name);
        Assert.True(result);
        Assert.NotNull(provider.ActiveProviderName);
        Assert.True(position.X >= 0 && position.Y >= 0);
    }

    private sealed class StubCursorPositionProvider : ICursorPositionProvider
    {
        public StubCursorPositionProvider(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public bool Available { get; set; } = true;

        public bool Succeeds { get; set; } = true;

        public ScreenPoint Position { get; set; } = new(100, 100);

        public int Calls { get; private set; }

        public bool IsAvailable => Available;

        public bool TryGetPosition(out ScreenPoint position)
        {
            Calls++;
            position = Position;
            return Succeeds;
        }
    }
}
