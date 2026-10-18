using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Platform;
using Catomania.Controls;
using Catomania.Models;
using Xunit;

namespace Catomania.Tests.Controls;

/// <summary>
/// Tests for <see cref="CatView" />. The cat is rendered by the headless platform and the pixels are checked.
/// </summary>
public sealed class CatViewTests
{
    private const double WindowSize = 100;

    private static readonly HeadlessUnitTestSession _session = HeadlessUnitTestSession.StartNew(typeof(TestAppBuilder));

    [Fact]
    public Task Render_EveryAnimationState_DrawsFur()
        => _session.Dispatch(() =>
        {
            var (catView, window) = CreateCatWindow();
            try
            {
                foreach (var state in Enum.GetValues<CatAnimationState>())
                {
                    catView.Update(new CatRenderState(state, true, 1.1, false));
                    var pixels = CapturePixels(window);
                    var furPixels = CountColor(pixels, catView.GetPalette().FurColor, 12);
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
                var eyeColor = catView.GetPalette().EyeColor;

                catView.Update(new CatRenderState(CatAnimationState.Idle, true, 1.1, false));
                var facingRight = GetColorCentroidX(CapturePixels(window), eyeColor, 40);

                catView.Update(new CatRenderState(CatAnimationState.Idle, false, 1.1, false));
                var facingLeft = GetColorCentroidX(CapturePixels(window), eyeColor, 40);

                Assert.True(facingRight > WindowSize / 2, $"eyes should be in the right half, got {facingRight}");
                Assert.True(facingLeft < WindowSize / 2, $"eyes should be in the left half, got {facingLeft}");
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
                catView.ColorScheme = CatColorScheme.Black;
                catView.Update(new CatRenderState(CatAnimationState.Walk, true, 2.2, false));
                var pixels = CapturePixels(window);

                Assert.True(CountColor(pixels, CatPalette.FromScheme(CatColorScheme.Black).FurColor, 12) > 40);
                Assert.Equal(0, CountColor(pixels, CatPalette.FromScheme(CatColorScheme.Ginger).FurColor, 12));
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
                var eyeColor = catView.GetPalette().EyeColor;

                catView.Update(new CatRenderState(CatAnimationState.Idle, true, 1.1, false));
                var eyesOpen = CountColor(CapturePixels(window), eyeColor, 40);

                catView.Update(new CatRenderState(CatAnimationState.Idle, true, 1.1, true));
                var eyesClosed = CountColor(CapturePixels(window), eyeColor, 40);

                Assert.True(eyesOpen > 0, "the eyes should be visible");
                Assert.Equal(0, eyesClosed);
            }
            finally
            {
                window.Close();
            }
        }, CancellationToken.None);

    [Fact]
    public void FromScheme_EveryScheme_ReturnsAPalette()
    {
        foreach (var scheme in Enum.GetValues<CatColorScheme>())
        {
            var palette = CatPalette.FromScheme(scheme);
            Assert.NotEqual(default, palette.FurColor);
            Assert.Same(palette, CatPalette.FromScheme(scheme));
        }
    }

    private static (CatView CatView, Window Window) CreateCatWindow()
    {
        var catView = new CatView();
        var window = new Window
        {
            Width = WindowSize,
            Height = WindowSize,
            Background = Brushes.Transparent,
            Content = catView,
        };
        window.Show();
        return (catView, window);
    }

    private static unsafe Color[] CapturePixels(Window window)
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);
        using var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        using var buffer = frame!.Lock();
        var size = buffer.Size;
        var pixels = new Color[size.Width * size.Height];
        var address = (byte*)buffer.Address.ToPointer();
        var isBgra = buffer.Format == PixelFormat.Bgra8888;
        for (var y = 0; y < size.Height; y++)
        {
            for (var x = 0; x < size.Width; x++)
            {
                var offset = (y * buffer.RowBytes) + (x * 4);
                var first = address[offset];
                var second = address[offset + 1];
                var third = address[offset + 2];
                var alpha = address[offset + 3];
                pixels[(y * size.Width) + x] = isBgra
                    ? Color.FromArgb(alpha, third, second, first)
                    : Color.FromArgb(alpha, first, second, third);
            }
        }

        return pixels;
    }

    private static int CountColor(Color[] pixels, Color color, int tolerance)
    {
        var count = 0;
        foreach (var pixel in pixels)
        {
            if (IsSameColor(pixel, color, tolerance))
            {
                count++;
            }
        }

        return count;
    }

    private static double GetColorCentroidX(Color[] pixels, Color color, int tolerance)
    {
        var sum = 0L;
        var count = 0;
        for (var index = 0; index < pixels.Length; index++)
        {
            if (!IsSameColor(pixels[index], color, tolerance))
            {
                continue;
            }

            sum += index % (int)WindowSize;
            count++;
        }

        Assert.True(count > 0, "no pixels of the requested color found");
        return (double)sum / count;
    }

    private static bool IsSameColor(Color pixel, Color color, int tolerance)
        => pixel.A > 8
           && Math.Abs(pixel.R - color.R) <= tolerance
           && Math.Abs(pixel.G - color.G) <= tolerance
           && Math.Abs(pixel.B - color.B) <= tolerance;
}
