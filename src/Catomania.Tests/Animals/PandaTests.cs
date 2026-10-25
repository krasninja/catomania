using Avalonia.Controls;
using Avalonia.Headless;
using Catomania.Animals;
using Catomania.Controls;
using Catomania.Models;
using Xunit;

namespace Catomania.Tests.Animals;

/// <summary>
/// Tests for <see cref="Panda" /> drawn by <see cref="AnimalView" />. The panda is rendered by the headless platform
/// and the pixels are checked.
/// </summary>
public sealed class PandaTests
{
    private static readonly HeadlessUnitTestSession _session = TestHeadlessSession.Instance;

    [Fact]
    public Task Render_EveryAnimationState_DrawsFur()
        => _session.Dispatch(() =>
        {
            var (pandaView, window) = CreatePandaWindow();
            try
            {
                foreach (var state in Enum.GetValues<AnimalAnimationState>())
                {
                    pandaView.Update(new AnimalRenderState(state, true, 1.1, false));
                    var pixels = AnimalPixels.Capture(window);
                    var furPixels = AnimalPixels.CountColor(pixels, pandaView.Animal.BodyColor, 12);
                    Assert.True(furPixels > 40, $"{state} draws too few fur pixels: {furPixels}");
                }
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    [Fact]
    public Task Render_FacingLeft_MirrorsThePanda()
        => _session.Dispatch(() =>
        {
            var (pandaView, window) = CreatePandaWindow();
            try
            {
                var eyeColor = pandaView.Animal.EyeColor;

                pandaView.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, false));
                var facingRight = AnimalPixels.GetColorCentroidX(AnimalPixels.Capture(window), eyeColor, 40);

                pandaView.Update(new AnimalRenderState(AnimalAnimationState.Idle, false, 1.1, false));
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
    public Task Render_Blinking_HidesTheEyes()
        => _session.Dispatch(() =>
        {
            var (pandaView, window) = CreatePandaWindow();
            try
            {
                var eyeColor = pandaView.Animal.EyeColor;

                pandaView.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, false));
                var eyesOpen = AnimalPixels.CountColor(AnimalPixels.Capture(window), eyeColor, 40);

                pandaView.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, true));
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
    public Task Create_PandaInSettings_CreatesPanda()
        => _session.Dispatch(() =>
        {
            var animal = Animal.Create(new AppSettings { Animal = AnimalKind.Panda, ColorScheme = CatColorScheme.Black });

            var panda = Assert.IsType<Panda>(animal);
            Assert.Equal(AnimalKind.Panda, panda.Kind);
        }, CancellationToken.None);

    [Fact]
    public Task CreateSimulatorOptions_ShortLegs_TakeShorterStridesThanTheCat()
        => _session.Dispatch(() =>
        {
            var panda = new Panda().CreateSimulatorOptions();
            var cat = new Cat().CreateSimulatorOptions();

            Assert.True(panda.WalkStrideLength < cat.WalkStrideLength);
            Assert.True(panda.RunStrideLength < cat.RunStrideLength);
        }, CancellationToken.None);

    private static (AnimalView PandaView, Window Window) CreatePandaWindow() => AnimalPixels.CreateWindow(new Panda());
}
