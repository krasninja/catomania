using System.Runtime.InteropServices;

namespace Catomania.Input;

/// <summary>
/// Native functions used to read the global cursor position.
/// </summary>
internal static partial class NativeMethods
{
    private const string CoreFoundationLibraryName = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const string CoreGraphicsLibraryName = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    [StructLayout(LayoutKind.Sequential)]
    internal struct CGPoint
    {
        public double X;

        public double Y;
    }

    [DllImport(CoreGraphicsLibraryName)]
    internal static extern IntPtr CGEventCreate(IntPtr source);

    [DllImport(CoreGraphicsLibraryName)]
    internal static extern CGPoint CGEventGetLocation(IntPtr pointer);

    [DllImport(CoreFoundationLibraryName)]
    internal static extern void CFRelease(IntPtr pointer);
}
