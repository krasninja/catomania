using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Platform;
using Catomania.Animals;
using Catomania.Controls;
using Xunit;

namespace Catomania.Tests.Animals;

/// <summary>
/// Renders an animal with the headless platform and inspects the pixels.
/// </summary>
internal static class AnimalPixels
{
    /// <summary>
    /// Size of the test window, equal to the design space so a design unit is a pixel.
    /// </summary>
    internal const double WindowSize = 100;

    internal static (AnimalView View, Window Window) CreateWindow(Animal animal)
    {
        var view = new AnimalView { Animal = animal };
        var window = new Window
        {
            Width = WindowSize,
            Height = WindowSize,
            Background = Brushes.Transparent,
            Content = view,
        };
        window.Show();
        return (view, window);
    }

    internal static unsafe Color[] Capture(Window window)
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

    internal static int CountColor(Color[] pixels, Color color, int tolerance)
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

    internal static double GetColorCentroidX(Color[] pixels, Color color, int tolerance)
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

    internal static bool IsSameColor(Color pixel, Color color, int tolerance)
        => pixel.A > 8
           && Math.Abs(pixel.R - color.R) <= tolerance
           && Math.Abs(pixel.G - color.G) <= tolerance
           && Math.Abs(pixel.B - color.B) <= tolerance;
}
