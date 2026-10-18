namespace Catomania.Simulation;

/// <summary>
/// A rectangle in physical screen coordinates (pixels).
/// </summary>
/// <param name="X">Left coordinate.</param>
/// <param name="Y">Top coordinate.</param>
/// <param name="Width">Rectangle width.</param>
/// <param name="Height">Rectangle height.</param>
public readonly record struct SimulationRect(double X, double Y, double Width, double Height)
{
    /// <summary>
    /// Gets an empty rectangle.
    /// </summary>
    public static SimulationRect Empty => new(0, 0, 0, 0);

    /// <summary>
    /// Gets the left coordinate.
    /// </summary>
    public double Left => X;

    /// <summary>
    /// Gets the top coordinate.
    /// </summary>
    public double Top => Y;

    /// <summary>
    /// Gets the right coordinate.
    /// </summary>
    public double Right => X + Width;

    /// <summary>
    /// Gets the bottom coordinate.
    /// </summary>
    public double Bottom => Y + Height;

    /// <summary>
    /// Gets a value indicating whether the rectangle has no area.
    /// </summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>
    /// Check whether the point is inside the rectangle.
    /// </summary>
    /// <param name="point">Point to check.</param>
    /// <returns><c>true</c> if the rectangle contains the point.</returns>
    public bool Contains(ScreenPoint point) => point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

    /// <summary>
    /// Shrink the rectangle from all sides.
    /// </summary>
    /// <param name="margin">Margin in pixels.</param>
    /// <returns>Deflated rectangle.</returns>
    public SimulationRect Deflate(double margin) => new(
        X + margin,
        Y + margin,
        Math.Max(0, Width - (margin * 2)),
        Math.Max(0, Height - (margin * 2)));

    /// <summary>
    /// Limit the point by the rectangle bounds.
    /// </summary>
    /// <param name="point">Point to clamp.</param>
    /// <returns>Clamped point.</returns>
    public ScreenPoint Clamp(ScreenPoint point)
    {
        if (IsEmpty)
        {
            return point;
        }

        return new ScreenPoint(
            Math.Clamp(point.X, Math.Min(Left, Right), Math.Max(Left, Right)),
            Math.Clamp(point.Y, Math.Min(Top, Bottom), Math.Max(Top, Bottom)));
    }

    /// <summary>
    /// Get the smallest rectangle that contains both rectangles.
    /// </summary>
    /// <param name="left">First rectangle.</param>
    /// <param name="right">Second rectangle.</param>
    /// <returns>Union rectangle.</returns>
    public static SimulationRect Union(SimulationRect left, SimulationRect right)
    {
        if (left.IsEmpty)
        {
            return right;
        }

        if (right.IsEmpty)
        {
            return left;
        }

        var x = Math.Min(left.Left, right.Left);
        var y = Math.Min(left.Top, right.Top);
        return new SimulationRect(
            x,
            y,
            Math.Max(left.Right, right.Right) - x,
            Math.Max(left.Bottom, right.Bottom) - y);
    }
}
