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
/// Tests for <see cref="Muchi" /> drawn by <see cref="AnimalView" />. Muchi is rendered by the headless platform
/// and the pixels are checked.
/// </summary>
public sealed class MuchiTests
{
    private static readonly HeadlessUnitTestSession _session = TestHeadlessSession.Instance;

    [Fact]
    public void CreateSimulatorOptions_Tricks_ShouldIncludeTheHuntAfterTheCommonTricks()
    {
        // Arrange & act.
        var options = new Muchi(MuchiFur.Orange).CreateSimulatorOptions();

        // Assert.
        Assert.Equal([.. new AnimalSimulatorOptions().Tricks, AnimalTrick.MuchiHunt], options.Tricks);
        Assert.True(options.TrickDurations[AnimalTrick.MuchiHunt] > 0);
    }

    [Fact]
    public Task Render_Run_UsesTheWalkingPoseNotTheHuntingCrouch()
        => _session.Dispatch(() =>
        {
            var (view, window) = AnimalPixels.CreateWindow(new Muchi(MuchiFur.Orange));
            try
            {
                var fur = view.Animal.BodyColor;
                var walkTop = GetTopRow(Render(view, window, AnimalAnimationState.Walk), fur);
                var runTop = GetTopRow(Render(view, window, AnimalAnimationState.Run), fur);
                var huntTop = GetTopRow(
                    Render(view, window, AnimalAnimationState.Idle, AnimalTrick.MuchiHunt, 0.3),
                    fur);

                Assert.InRange(runTop, walkTop - 3, walkTop + 3);
                Assert.True(huntTop > runTop + 5, $"the hunting crouch should be lower: {huntTop} vs {runTop}");
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    [Fact]
    public Task Render_HuntTrick_PouncesUpAtTheEnd()
        => _session.Dispatch(() =>
        {
            var (view, window) = AnimalPixels.CreateWindow(new Muchi(MuchiFur.Orange));
            try
            {
                var fur = view.Animal.BodyColor;
                var stalkTop = GetTopRow(
                    Render(view, window, AnimalAnimationState.Idle, AnimalTrick.MuchiHunt, 0.3),
                    fur);
                var pounceTop = GetTopRow(
                    Render(view, window, AnimalAnimationState.Idle, AnimalTrick.MuchiHunt, 0.87),
                    fur);

                Assert.True(pounceTop < stalkTop - 5, $"Muchi should be in the air: {pounceTop} vs {stalkTop}");
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    private static Color[] Render(
        AnimalView view,
        Window window,
        AnimalAnimationState state,
        AnimalTrick trick = AnimalTrick.None,
        double progress = 0)
    {
        view.Update(new AnimalRenderState(state, true, 1.1, false, trick, progress));
        return AnimalPixels.Capture(window);
    }

    // The highest row with the fur color, the top of the head or the ears.
    private static int GetTopRow(Color[] pixels, Color color)
    {
        var size = (int)AnimalPixels.WindowSize;
        for (var i = 0; i < pixels.Length; i++)
        {
            if (AnimalPixels.IsSameColor(pixels[i], color, 12))
            {
                return i / size;
            }
        }

        return size;
    }
}
