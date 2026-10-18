using System.Runtime.InteropServices;

namespace Catomania.Input;

/// <summary>
/// Native functions used to read the global cursor position.
/// </summary>
internal static partial class NativeMethods
{
    private const string X11LibraryName = "libX11.so.6";

    [DllImport(X11LibraryName)]
    internal static extern IntPtr XOpenDisplay(IntPtr displayName);

    [DllImport(X11LibraryName)]
    internal static extern int XCloseDisplay(IntPtr display);

    [DllImport(X11LibraryName)]
    internal static extern IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport(X11LibraryName)]
    internal static extern int XQueryPointer(
        IntPtr display,
        IntPtr window,
        out IntPtr rootReturn,
        out IntPtr childReturn,
        out int rootX,
        out int rootY,
        out int windowX,
        out int windowY,
        out uint maskReturn);
}
