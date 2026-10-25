using Avalonia.Controls;
using Avalonia.Headless;
using Catomania.Animals;
using Catomania.Controls;
using Catomania.Models;
using Xunit;

namespace Catomania.Tests.Animals;

/// <summary>
/// Tests for <see cref="Dog" /> drawn by <see cref="AnimalView" />. The dog is rendered by the headless platform
/// and the pixels are checked.
/// </summary>
public sealed class DogTests
{
    private static readonly HeadlessUnitTestSession _session = TestHeadlessSession.Instance;

    [Fact]
    public Task Render_EveryAnimationState_DrawsFur()
        => _session.Dispatch(() =>
        {
            var (dogView, window) = CreateDogWindow();
            try
            {
                foreach (var state in Enum.GetValues<AnimalAnimationState>())
                {
                    dogView.Update(new AnimalRenderState(state, true, 1.1, false));
                    var pixels = AnimalPixels.Capture(window);
                    var furPixels = AnimalPixels.CountColor(pixels, dogView.Animal.BodyColor, 12);
                    Assert.True(furPixels > 40, $"{state} draws too few fur pixels: {furPixels}");
                }
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    [Fact]
    public Task Render_FacingLeft_MirrorsTheDog()
        => _session.Dispatch(() =>
        {
            var (dogView, window) = CreateDogWindow();
            try
            {
                var eyeColor = dogView.Animal.EyeColor;

                dogView.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, false));
                var facingRight = AnimalPixels.GetColorCentroidX(AnimalPixels.Capture(window), eyeColor, 40);

                dogView.Update(new AnimalRenderState(AnimalAnimationState.Idle, false, 1.1, false));
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
            var (dogView, window) = CreateDogWindow();
            try
            {
                var eyeColor = dogView.Animal.EyeColor;

                dogView.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, false));
                var eyesOpen = AnimalPixels.CountColor(AnimalPixels.Capture(window), eyeColor, 40);

                dogView.Update(new AnimalRenderState(AnimalAnimationState.Idle, true, 1.1, true));
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
    public Task Create_DogInSettings_CreatesDog()
        => _session.Dispatch(() =>
        {
            var animal = Animal.Create(new AppSettings { Animal = AnimalKind.Dog, ColorScheme = CatColorScheme.Black });

            var dog = Assert.IsType<Dog>(animal);
            Assert.Equal(AnimalKind.Dog, dog.Kind);
        }, CancellationToken.None);

    [Fact]
    public Task CreateSimulatorOptions_LongLegs_TakeLongerStridesThanTheCat()
        => _session.Dispatch(() =>
        {
            var dog = new Dog().CreateSimulatorOptions();
            var cat = new Cat().CreateSimulatorOptions();

            Assert.True(dog.WalkStrideLength > cat.WalkStrideLength);
            Assert.True(dog.RunStrideLength > cat.RunStrideLength);
        }, CancellationToken.None);

    private static (AnimalView DogView, Window Window) CreateDogWindow() => AnimalPixels.CreateWindow(new Dog());
}
