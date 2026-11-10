using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Catomania.Animals;
using Catomania.Controls;
using Catomania.Models;
using Catomania.Simulation;
using Xunit;

namespace Catomania.Tests.Animals;

/// <summary>
/// Tests for <see cref="Nolu" /> drawn by <see cref="AnimalView" />. Every fur is rendered by the headless
/// platform and the fur and head pixels are checked.
/// </summary>
public sealed class NoluTests
{
    private static readonly HeadlessUnitTestSession _session = TestHeadlessSession.Instance;

    public static TheoryData<NoluFur> Furs => new(Enum.GetValues<NoluFur>());

    [Theory]
    [MemberData(nameof(Furs))]
    public Task Render_EveryAnimationState_DrawsTheSelectedFurAndHeadColors(NoluFur fur)
        => _session.Dispatch(() =>
        {
            var (view, window) = AnimalPixels.CreateWindow(new Nolu(fur));
            try
            {
                var headColor = GetHeadColor(fur);
                foreach (var state in Enum.GetValues<AnimalAnimationState>())
                {
                    view.Update(new AnimalRenderState(state, true, 1.1, false));
                    var pixels = AnimalPixels.Capture(window);
                    var furPixels = AnimalPixels.CountColor(pixels, view.Animal.BodyColor, 12);
                    var headPixels = AnimalPixels.CountColor(pixels, headColor, 12);
                    Assert.True(furPixels > 40, $"{state} draws too few fur pixels: {furPixels}");
                    Assert.True(headPixels > 40, $"{state} draws too few head pixels: {headPixels}");
                }
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    [Theory]
    [MemberData(nameof(Furs))]
    public void Create_NoluInSettings_CreatesNoluWithTheSelectedFur(NoluFur fur)
    {
        var animal = Animal.Create(new AppSettings { Animal = AnimalKind.Nolu, NoluFur = fur });

        var nolu = Assert.IsType<Nolu>(animal);
        Assert.Equal(fur, nolu.Fur);
        Assert.Equal(GetFurColor(fur), nolu.BodyColor);
    }

    private static Color GetFurColor(NoluFur fur) => Color.FromUInt32(fur switch
    {
        NoluFur.Violet => 0xFF5A4E98,
        NoluFur.Green => 0xFF61833F,
        _ => 0xFF6D5353,
    });

    private static Color GetHeadColor(NoluFur fur) => Color.FromUInt32(fur switch
    {
        NoluFur.Violet => 0xFFAE99D7,
        NoluFur.Green => 0xFFB1BC83,
        _ => 0xFFB89E96,
    });
}
