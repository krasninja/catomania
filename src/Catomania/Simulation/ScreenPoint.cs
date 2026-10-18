namespace Catomania.Simulation;

/// <summary>
/// A point in physical screen coordinates: pixels, origin at the top-left corner of the virtual screen.
/// </summary>
/// <param name="X">Horizontal coordinate.</param>
/// <param name="Y">Vertical coordinate.</param>
public readonly record struct ScreenPoint(double X, double Y)
{
    /// <summary>
    /// Gets the origin point.
    /// </summary>
    public static ScreenPoint Zero => new(0, 0);

    /// <summary>
    /// Gets the length of the vector from the origin to this point.
    /// </summary>
    public double Length => Math.Sqrt((X * X) + (Y * Y));

    /// <summary>
    /// Adds two points.
    /// </summary>
    public static ScreenPoint operator +(ScreenPoint left, ScreenPoint right) => new(left.X + right.X, left.Y + right.Y);

    /// <summary>
    /// Subtracts one point from another.
    /// </summary>
    public static ScreenPoint operator -(ScreenPoint left, ScreenPoint right) => new(left.X - right.X, left.Y - right.Y);

    /// <summary>
    /// Multiplies the point coordinates by a factor.
    /// </summary>
    public static ScreenPoint operator *(ScreenPoint point, double factor) => new(point.X * factor, point.Y * factor);

    /// <summary>
    /// Divides the point coordinates by a divisor.
    /// </summary>
    public static ScreenPoint operator /(ScreenPoint point, double divisor) => new(point.X / divisor, point.Y / divisor);

    /// <summary>
    /// Get distance between two points.
    /// </summary>
    /// <param name="left">First point.</param>
    /// <param name="right">Second point.</param>
    /// <returns>Distance in pixels.</returns>
    public static double Distance(ScreenPoint left, ScreenPoint right) => (left - right).Length;

    /// <summary>
    /// Get a unit vector pointing in the same direction.
    /// </summary>
    /// <returns>Normalized point.</returns>
    public ScreenPoint Normalized()
    {
        var length = Length;
        return length < double.Epsilon ? Zero : new ScreenPoint(X / length, Y / length);
    }

    /// <summary>
    /// Get a point with rounded coordinates.
    /// </summary>
    /// <returns>Rounded point.</returns>
    public ScreenPoint Rounded() => new(Math.Round(X), Math.Round(Y));
}
