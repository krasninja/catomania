using Avalonia.Controls;
using Avalonia.Headless;
using Catomania.Animals;
using Catomania.Controls;
using Catomania.Models;
using Xunit;

namespace Catomania.Tests.Animals;

/// <summary>
/// Tests for <see cref="Cat" /> drawn by <see cref="AnimalView" />. The cat is rendered by the headless platform
/// and the pixels are checked.
/// </summary>
public sealed class CatTests
{
    private static readonly HeadlessUnitTestSession _session = TestHeadlessSession.Instance;

    [Fact]
    public Task Render_EveryAnimationState_DrawsFur()
        => _session.Dispatch(() =>
        {
            var (catView, window) = CreateCatWindow();
            try
            {
                foreach (var state in Enum.GetValues<AnimalAnimationState>())
                {
                    catView.Update(new AnimalRenderState(state, true, 1.1, false));
                    var pixels = AnimalPixels.Capture(window);
                    var furPixels = AnimalPixels.CountColor(pixels, catView.Animal.BodyColor, 12);
                    Assert.True(furPixels > 40, $"{state} draws too few fur pixels: {furPixels}");
                }
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    [Fact]
    public Task Render_FacingLeft_MirrorsTheCat()
        => _session.Dispatch(() =>
        {
            var (catView, window) = CreateCatWindow();
            try
            {
                var eyeColor = catView.Animal.EyeColor;

                catView.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, false));
                var facingRight = AnimalPixels.GetColorCentroidX(AnimalPixels.Capture(window), eyeColor, 40);

                catView.Update(new AnimalRenderState(AnimalAnimationState.Idle, false, 1.1, false));
                var facingLeft = AnimalPixels.GetColorCentroidX(AnimalPixels.Capture(window), eyeColor, 40);

                Assert.True(facingRight > AnimalPixels.WindowSize / 2, $"eyes should be in the right half, got {facingRight}");
                Assert.True(facingLeft < AnimalPixels.WindowSize / 2, $"eyes should be in the left half, got {facingLeft}");
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    [Fact]
    public Task Render_BlackScheme_DrawsBlackFur()
        => _session.Dispatch(() =>
        {
            var (catView, window) = CreateCatWindow();
            try
            {
                catView.Animal = new Cat(CatColorScheme.Black);
                catView.Update(new AnimalRenderState(AnimalAnimationState.Walk, true, 2.2, false));
                var pixels = AnimalPixels.Capture(window);

                Assert.True(AnimalPixels.CountColor(pixels, CatPalette.FromScheme(CatColorScheme.Black).FurColor, 12) > 40);
                Assert.Equal(0, AnimalPixels.CountColor(pixels, CatPalette.FromScheme(CatColorScheme.Ginger).FurColor, 12));
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    [Fact]
    public Task Render_Blinking_HidesTheEyes()
        => _session.Dispatch(() =>
        {
            var (catView, window) = CreateCatWindow();
            try
            {
                var eyeColor = catView.Animal.EyeColor;

                catView.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, false));
                var eyesOpen = AnimalPixels.CountColor(AnimalPixels.Capture(window), eyeColor, 40);

                catView.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, true));
                var eyesClosed = AnimalPixels.CountColor(AnimalPixels.Capture(window), eyeColor, 40);

                Assert.True(eyesOpen > 0, "the eyes should be visible");
                Assert.Equal(0, eyesClosed);
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    [Fact]
    public Task Create_DefaultSettings_CreatesGingerCat()
        => _session.Dispatch(() =>
        {
            var animal = Animal.Create(new AppSettings());

            var cat = Assert.IsType<Cat>(animal);
            Assert.Equal(AnimalKind.Cat, cat.Kind);
            Assert.Equal(CatColorScheme.Ginger, cat.ColorScheme);
        }, CancellationToken.None);

    [Fact]
    public Task Create_ColorSchemeInSettings_UsesTheScheme()
        => _session.Dispatch(() =>
        {
            var animal = Animal.Create(new AppSettings { ColorScheme = CatColorScheme.Cream });

            Assert.Equal(CatPalette.FromScheme(CatColorScheme.Cream).FurColor, animal.BodyColor);
        }, CancellationToken.None);

    [Fact]
    public Task FromScheme_EveryScheme_ReturnsAPalette()
        => _session.Dispatch(() =>
        {
            foreach (var scheme in Enum.GetValues<CatColorScheme>())
            {
                var palette = CatPalette.FromScheme(scheme);
                Assert.NotEqual(default, palette.FurColor);
                Assert.Same(palette, CatPalette.FromScheme(scheme));
            }
        }, CancellationToken.None);

    private static (AnimalView CatView, Window Window) CreateCatWindow() => AnimalPixels.CreateWindow(new Cat());
}
