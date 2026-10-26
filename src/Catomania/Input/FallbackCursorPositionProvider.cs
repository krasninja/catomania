using Catomania.Abstractions;
using Catomania.Simulation;

namespace Catomania.Input;

/// <summary>
/// Remembers the cursor position reported by the pointer events of the animal window itself.
/// It is used when the global cursor position is not available on the platform, so at least
/// the animal follows the pointer while it is over the animal.
/// </summary>
public sealed class FallbackCursorPositionProvider : ICursorPositionProvider
{
    private ScreenPoint? _position;

    /// <inheritdoc />
    public string Name => "Window";

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <summary>
    /// Remember the cursor position.
    /// </summary>
    /// <param name="position">Cursor position in physical screen pixels.</param>
    public void Report(ScreenPoint position) => _position = position;

    /// <inheritdoc />
    public bool TryGetPosition(out ScreenPoint position)
    {
        if (_position is { } reportedPosition)
        {
            position = reportedPosition;
            return true;
        }

        position = ScreenPoint.Zero;
        return false;
    }
}
