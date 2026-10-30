using Catomania.Animals;
using Catomania.Models;
using Xunit;

namespace Catomania.Tests.Animals;

/// <summary>
/// Tests for the tricks applied by <see cref="Animal.Render" /> to every animal.
/// </summary>
public sealed class AnimalTrickTests
{
    private static readonly Avalonia.Headless.HeadlessUnitTestSession _session = TestHeadlessSession.Instance;

    [Fact]
    public Task Render_EveryTrick_DrawsEveryAnimal()
        => _session.Dispatch(() =>
        {
            foreach (var kind in Enum.GetValues<AnimalKind>())
            {
                var (view, window) = AnimalPixels.CreateWindow(Animal.Create(new AppSettings { Animal = kind }));
                try
                {
                    foreach (var trick in Enum.GetValues<AnimalTrick>())
                    {
                        // Avoids 0.35 and 0.65, the spinning animal is seen edge-on and is too thin then.
                        foreach (var progress in new[] { 0.1, 0.3, 0.5, 0.7, 0.9 })
                        {
                            view.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, false, trick, progress));
                            var pixels = AnimalPixels.Capture(window);
                            var bodyPixels = AnimalPixels.CountColor(pixels, view.Animal.BodyColor, 12);
                            Assert.True(
                                bodyPixels > 40,
                                $"{kind} {trick} at {progress} draws too few body pixels: {bodyPixels}");
                        }
                    }
                }
                finally
                {
                    window.Close();
                }
            }
        }, CancellationToken.None);
}
