using System.Runtime.InteropServices;
using Catomania.Abstractions;
using Catomania.Simulation;

namespace Catomania.Input;

/// <summary>
/// Reads the global cursor position on X11 (Linux and BSD) with the <c>XQueryPointer</c> call.
/// It works through XWayland as well.
/// </summary>
public sealed class X11CursorPositionProvider : ICursorPositionProvider, IDisposable
{
    private readonly Lock _syncRoot = new();

    private IntPtr _display;
    private IntPtr _rootWindow;
    private bool _unavailable;
    private bool _disposed;

    /// <inheritdoc />
    public string Name => "X11";

    /// <inheritdoc />
    public bool IsAvailable
    {
        get
        {
            if (!IsSupportedPlatform())
            {
                return false;
            }

            return EnsureInitialized();
        }
    }

    /// <inheritdoc />
    public bool TryGetPosition(out ScreenPoint position)
    {
        position = ScreenPoint.Zero;
        if (!EnsureInitialized())
        {
            return false;
        }

        lock (_syncRoot)
        {
            // The display may have been closed after the check above, Xlib crashes on a null display.
            if (_display == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                var status = NativeMethods.XQueryPointer(
                    _display,
                    _rootWindow,
                    out _,
                    out _,
                    out var rootX,
                    out var rootY,
                    out _,
                    out _,
                    out _);
                if (status == 0)
                {
                    return false;
                }

                position = new ScreenPoint(rootX, rootY);
                return true;
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
                _unavailable = true;
                return false;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        CloseDisplay();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="X11CursorPositionProvider" /> class.
    /// </summary>
    ~X11CursorPositionProvider()
    {
        CloseDisplay();
    }

    private static bool IsSupportedPlatform()
        => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);

    private bool EnsureInitialized()
    {
        if (_display != IntPtr.Zero)
        {
            return true;
        }

        if (_unavailable || _disposed)
        {
            return false;
        }

        lock (_syncRoot)
        {
            if (_display != IntPtr.Zero)
            {
                return true;
            }

            if (_unavailable || _disposed || !IsSupportedPlatform())
            {
                _unavailable = true;
                return false;
            }

            try
            {
                _display = NativeMethods.XOpenDisplay(IntPtr.Zero);
                if (_display == IntPtr.Zero)
                {
                    _unavailable = true;
                    return false;
                }

                _rootWindow = NativeMethods.XDefaultRootWindow(_display);
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
                _display = IntPtr.Zero;
                _unavailable = true;
                return false;
            }

            return true;
        }
    }

    private void CloseDisplay()
    {
        lock (_syncRoot)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_display != IntPtr.Zero)
            {
                NativeMethods.XCloseDisplay(_display);
                _display = IntPtr.Zero;
            }
        }
    }
}
