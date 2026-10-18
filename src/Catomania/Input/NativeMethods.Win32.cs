using System.Runtime.InteropServices;

namespace Catomania.Input;

/// <summary>
/// Native functions used to read the global cursor position.
/// </summary>
internal static partial class NativeMethods
{
    private const string Win32LibraryName = "user32.dll";

    [StructLayout(LayoutKind.Sequential)]
    internal struct Win32Point
    {
        public int X;

        public int Y;
    }

    [DllImport(Win32LibraryName, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCursorPos(out Win32Point point);
}
