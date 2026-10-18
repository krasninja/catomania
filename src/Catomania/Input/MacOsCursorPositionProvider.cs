using System.Runtime.InteropServices;
using Catomania.Simulation;

namespace Catomania.Input;

/// <summary>
/// Reads the global cursor position on macOS with Core Graphics.
/// Core Graphics reports points, so the position is multiplied by the screen scaling.
/// </summary>
public sealed class MacOsCursorPositionProvider : ICursorPositionProvider
{
    private readonly Func<double> _scalingProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MacOsCursorPositionProvider" /> class.
    /// </summary>
    /// <param name="scalingProvider">Provides the screen scaling used to convert points to pixels.</param>
    public MacOsCursorPositionProvider(Func<double>? scalingProvider = null)
    {
        _scalingProvider = scalingProvider ?? (() => 1.0);
    }

    /// <inheritdoc />
    public string Name => "macOS";

    /// <inheritdoc />
    public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    /// <inheritdoc />
    public bool TryGetPosition(out ScreenPoint position)
    {
        position = ScreenPoint.Zero;
        if (!IsAvailable)
        {
            return false;
        }

        var pointer = IntPtr.Zero;
        try
        {
            pointer = NativeMethods.CGEventCreate(IntPtr.Zero);
            if (pointer == IntPtr.Zero)
            {
                return false;
            }

            var location = NativeMethods.CGEventGetLocation(pointer);
            var scaling = _scalingProvider();
            if (scaling <= 0)
            {
                scaling = 1.0;
            }

            position = new ScreenPoint(location.X * scaling, location.Y * scaling);
            return true;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
        finally
        {
            if (pointer != IntPtr.Zero)
            {
                NativeMethods.CGEventRelease(pointer);
            }
        }
    }
}
