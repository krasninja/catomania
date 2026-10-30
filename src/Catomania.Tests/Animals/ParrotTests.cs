using Avalonia.Controls;
using Avalonia.Headless;
using Catomania.Animals;
using Catomania.Controls;
using Catomania.Models;
using Xunit;

namespace Catomania.Tests.Animals;

/// <summary>
/// Tests for <see cref="Parrot" /> drawn by <see cref="AnimalView" />. The parrot is rendered by the headless platform
/// and the pixels are checked, for every plumage.
/// </summary>
public sealed class ParrotTests
{
    private static readonly HeadlessUnitTestSession _session = TestHeadlessSession.Instance;

    public static TheoryData<ParrotPlumage> Plumages => new(Enum.GetValues<ParrotPlumage>());

    [Theory]
    [MemberData(nameof(Plumages))]
    public Task Render_EveryAnimationState_DrawsPlumage(ParrotPlumage plumage)
        => _session.Dispatch(() =>
        {
            var (parrotView, window) = CreateParrotWindow(plumage);
            try
            {
                foreach (var state in Enum.GetValues<AnimalAnimationState>())
                {
                    parrotView.Update(new AnimalRenderState(state, true, 1.1, false));
                    var pixels = AnimalPixels.Capture(window);
                    var plumagePixels = AnimalPixels.CountColor(pixels, parrotView.Animal.BodyColor, 12);
                    Assert.True(plumagePixels > 40, $"{state} draws too few plumage pixels: {plumagePixels}");
                }
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    [Theory]
    [MemberData(nameof(Plumages))]
    public Task Render_FacingLeft_MirrorsTheParrot(ParrotPlumage plumage)
        => _session.Dispatch(() =>
        {
            var (parrotView, window) = CreateParrotWindow(plumage);
            try
            {
                var eyeColor = parrotView.Animal.EyeColor;

                parrotView.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, false));
                var facingRight = AnimalPixels.GetColorCentroidX(AnimalPixels.Capture(window), eyeColor, 40);

                parrotView.Update(new AnimalRenderState(AnimalAnimationState.Idle, false, 1.1, false));
                var facingLeft = AnimalPixels.GetColorCentroidX(AnimalPixels.Capture(window), eyeColor, 40);

                Assert.True(facingRight > AnimalPixels.WindowSize / 2, $"eyes should be in the right half, got {facingRight}");
                Assert.True(facingLeft < AnimalPixels.WindowSize / 2, $"eyes should be in the left half, got {facingLeft}");
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    [Theory]
    [MemberData(nameof(Plumages))]
    public Task Render_Blinking_HidesTheEyes(ParrotPlumage plumage)
        => _session.Dispatch(() =>
        {
            var (parrotView, window) = CreateParrotWindow(plumage);
            try
            {
                var eyeColor = parrotView.Animal.EyeColor;

                parrotView.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, false));
                var eyesOpen = AnimalPixels.CountColor(AnimalPixels.Capture(window), eyeColor, 40);

                parrotView.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, true));
                var eyesClosed = AnimalPixels.CountColor(AnimalPixels.Capture(window), eyeColor, 40);

                Assert.True(eyesOpen > 0, "the eyes should be visible");
                Assert.Equal(0, eyesClosed);
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    [Theory]
    [MemberData(nameof(Plumages))]
    public Task Create_ParrotInSettings_CreatesParrotWithThePlumage(ParrotPlumage plumage)
        => _session.Dispatch(() =>
        {
            var animal = Animal.Create(new AppSettings { Animal = AnimalKind.Parrot, ParrotPlumage = plumage });

            var parrot = Assert.IsType<Parrot>(animal);
            Assert.Equal(AnimalKind.Parrot, parrot.Kind);
            Assert.Equal(plumage, parrot.Plumage);
            Assert.Same(ParrotPalette.FromPlumage(plumage), parrot.Palette);
        }, CancellationToken.None);

    [Fact]
    public Task Render_EveryAnimationState_StaysInsideTheDesignSpace()
        => _session.Dispatch(() =>
        {
            var (parrotView, window) = CreateParrotWindow(ParrotPlumage.Scarlet);
            try
            {
                // The raised wings and the long tail must not be cut off at the edges.
                var size = (int)AnimalPixels.WindowSize;
                foreach (var state in Enum.GetValues<AnimalAnimationState>())
                {
                    for (var phase = 0.0; phase < Math.PI * 4; phase += 0.25)
                    {
                        parrotView.Update(new AnimalRenderState(state, true, phase, false));
                        var pixels = AnimalPixels.Capture(window);
                        var background = pixels[^1];
                        var topRow = pixels.Take(size).Count(pixel => pixel != background);
                        var leftColumn = Enumerable.Range(0, size).Count(y => pixels[y * size] != background);
                        Assert.True(topRow == 0, $"{state} at phase {phase} draws {topRow} pixels on the top row");
                        Assert.True(leftColumn == 0, $"{state} at phase {phase} draws {leftColumn} pixels on the left column");
                    }
                }
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    private static (AnimalView ParrotView, Window Window) CreateParrotWindow(ParrotPlumage plumage)
        => AnimalPixels.CreateWindow(new Parrot(plumage));
}
