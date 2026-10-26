using Catomania.Simulation;

namespace Catomania.Abstractions;

/// <summary>
/// Provides the global cursor position.
/// </summary>
public interface ICursorPositionProvider
{
    /// <summary>
    /// Gets the provider name, used for diagnostics.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the provider can be used on the current platform.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Read the cursor position.
    /// </summary>
    /// <param name="position">Cursor position in physical screen pixels.</param>
    /// <returns><c>true</c> when the position has been read.</returns>
    bool TryGetPosition(out ScreenPoint position);
}
