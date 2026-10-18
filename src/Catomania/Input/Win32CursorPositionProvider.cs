using System.Runtime.InteropServices;
using Catomania.Simulation;

namespace Catomania.Input;

/// <summary>
/// Reads the global cursor position on Windows with the <c>GetCursorPos</c> call.
/// </summary>
public sealed class Win32CursorPositionProvider : ICursorPositionProvider
{
    /// <inheritdoc />
    public string Name => "Win32";

    /// <inheritdoc />
    public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    /// <inheritdoc />
    public bool TryGetPosition(out ScreenPoint position)
    {
        position = ScreenPoint.Zero;
        if (!IsAvailable)
        {
            return false;
        }

        try
        {
            if (!NativeMethods.GetCursorPos(out var nativePoint))
            {
                return false;
            }

            position = new ScreenPoint(nativePoint.X, nativePoint.Y);
            return true;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }
}
