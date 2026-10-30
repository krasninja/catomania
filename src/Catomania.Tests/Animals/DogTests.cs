using Avalonia.Controls;
using Avalonia.Headless;
using Catomania.Animals;
using Catomania.Controls;
using Catomania.Models;
using Xunit;

namespace Catomania.Tests.Animals;

/// <summary>
/// Tests for <see cref="Dog" /> drawn by <see cref="AnimalView" />. The dog is rendered by the headless platform
/// and the pixels are checked, for every coat.
/// </summary>
public sealed class DogTests
{
    private static readonly HeadlessUnitTestSession _session = TestHeadlessSession.Instance;

    public static TheoryData<DogCoat> Coats => new(Enum.GetValues<DogCoat>());

    [Theory]
    [MemberData(nameof(Coats))]
    public Task Render_EveryAnimationState_DrawsFur(DogCoat coat)
        => _session.Dispatch(() =>
        {
            var (dogView, window) = CreateDogWindow(coat);
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

    [Theory]
    [MemberData(nameof(Coats))]
    public Task Render_FacingLeft_MirrorsTheDog(DogCoat coat)
        => _session.Dispatch(() =>
        {
            var (dogView, window) = CreateDogWindow(coat);
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

    [Theory]
    [MemberData(nameof(Coats))]
    public Task Render_Blinking_HidesTheEyes(DogCoat coat)
        => _session.Dispatch(() =>
        {
            var (dogView, window) = CreateDogWindow(coat);
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

    [Theory]
    [MemberData(nameof(Coats))]
    public Task Create_DogCoatInSettings_CreatesDogWithTheCoat(DogCoat coat)
        => _session.Dispatch(() =>
        {
            var animal = Animal.Create(new AppSettings { Animal = AnimalKind.Dog, DogCoat = coat });

            var dog = Assert.IsType<Dog>(animal);
            Assert.Equal(coat, dog.Coat);
            Assert.Same(DogPalette.FromCoat(coat), dog.Palette);
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

    private static (AnimalView DogView, Window Window) CreateDogWindow(DogCoat coat) => AnimalPixels.CreateWindow(new Dog(coat));
}
