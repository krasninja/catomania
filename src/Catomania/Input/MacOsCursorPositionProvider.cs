using System.Runtime.InteropServices;
using Catomania.Abstractions;
using Catomania.Simulation;

namespace Catomania.Input;

/// <summary>
/// Reads the global cursor position on macOS with Core Graphics.
/// Core Graphics reports points with the origin at the top-left corner of the main display. Avalonia positions
/// windows and reports screens on macOS in the same points (its desktop scaling is 1), so the position is used
/// as is. Multiplying it by the Retina backing scale would make the animal chase twice the cursor position.
/// </summary>
public sealed class MacOsCursorPositionProvider : ICursorPositionProvider
{
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
            position = new ScreenPoint(location.X, location.Y);
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
                NativeMethods.CFRelease(pointer);
            }
        }
    }
}
