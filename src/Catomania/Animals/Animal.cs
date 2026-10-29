using Avalonia;
using Avalonia.Media;
using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Animals;

/// <summary>
/// Base class of the animals that chase the cursor. The chase behavior is shared and implemented
/// by <see cref="AnimalSimulator" />, an animal tunes it with its own options and draws itself
/// with vector shapes in a <see cref="DesignSize" /> x <see cref="DesignSize" /> design space,
/// facing right. Scaling and mirroring are done by the caller.
/// </summary>
public abstract class Animal
{
    /// <summary>
    /// Size of the design space the animal is drawn in.
    /// </summary>
    public const double DesignSize = 100;

    /// <summary>
    /// Vertical coordinate of the ground the animal stands on, in the design space.
    /// </summary>
    protected const double GroundY = 92;

    /// <summary>
    /// Full circle in radians.
    /// </summary>
    protected const double TwoPi = Math.PI * 2;

    /// <summary>
    /// Gets the kind of the animal.
    /// </summary>
    public abstract AnimalKind Kind { get; }

    /// <summary>
    /// Gets the main body color, the most common color of the drawing.
    /// </summary>
    public abstract Color BodyColor { get; }

    /// <summary>
    /// Gets the eye color.
    /// </summary>
    public abstract Color EyeColor { get; }

    /// <summary>
    /// Gets the sound the animal makes.
    /// </summary>
    public abstract string Sound { get; }

    /// <summary>
    /// Create an animal of the kind configured in the settings.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    /// <returns>Animal.</returns>
    public static Animal Create(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        // Add new animals here. Unknown kinds fall back to the cat.
        return settings.Animal switch
        {
            AnimalKind.Panda => new Panda(),
            AnimalKind.Dog => new Dog(),
            AnimalKind.Snake => new Snake(),
            _ => new Cat(settings.ColorScheme),
        };
    }

    /// <summary>
    /// Create the simulation options that define how the animal moves.
    /// </summary>
    /// <returns>Simulation options.</returns>
    public virtual AnimalSimulatorOptions CreateSimulatorOptions() => new();

    /// <summary>
    /// Draw a frame of the animal in the design space, facing right.
    /// </summary>
    /// <param name="context">Drawing context.</param>
    /// <param name="state">Render state produced by the simulation.</param>
    public abstract void Render(DrawingContext context, AnimalRenderState state);

    /// <summary>
    /// Create a filled triangle.
    /// </summary>
    /// <param name="a">First vertex.</param>
    /// <param name="b">Second vertex.</param>
    /// <param name="c">Third vertex.</param>
    /// <returns>Triangle geometry.</returns>
    protected static Geometry CreateTriangle(Point a, Point b, Point c)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(a, true);
        context.LineTo(b);
        context.LineTo(c);
        context.EndFigure(true);

        return geometry;
    }

    /// <summary>
    /// Create an open cubic Bezier curve.
    /// </summary>
    /// <param name="start">Start point.</param>
    /// <param name="controlPoint1">First control point.</param>
    /// <param name="controlPoint2">Second control point.</param>
    /// <param name="end">End point.</param>
    /// <returns>Curve geometry.</returns>
    protected static Geometry CreateCurve(Point start, Point controlPoint1, Point controlPoint2, Point end)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(start, false);
            context.CubicBezierTo(controlPoint1, controlPoint2, end);
            context.EndFigure(false);
        }

        return geometry;
    }

    /// <summary>
    /// Draw the "zzz" floating up from a sleeping animal.
    /// </summary>
    /// <param name="context">Drawing context.</param>
    /// <param name="pen">Pen of the letters.</param>
    /// <param name="origin">Where the letters start, in the design space.</param>
    /// <param name="phase">Animation phase in radians.</param>
    protected static void DrawZzz(DrawingContext context, IPen pen, Point origin, double phase)
    {
        for (var i = 0; i < 3; i++)
        {
            var progress = ((phase / TwoPi) + (i / 3.0)) % 1.0;
            var x = origin.X + (progress * 16) + i;
            var y = origin.Y - (progress * 20) - (i * 3);
            var size = 3 + (i * 1.1);
            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(new Point(x, y), false);
                geometryContext.LineTo(new Point(x + size, y));
                geometryContext.LineTo(new Point(x, y + size));
                geometryContext.LineTo(new Point(x + size, y + size));
                geometryContext.EndFigure(false);
            }

            using (context.PushOpacity(Math.Sin(progress * Math.PI)))
            {
                context.DrawGeometry(null, pen, geometry);
            }
        }
    }

    /// <summary>
    /// Get the foot position relative to the point on the ground under the hip.
    /// </summary>
    /// <param name="phase">Gait cycle phase of the leg in radians.</param>
    /// <param name="gait">Leg motion of the gait.</param>
    /// <returns>Horizontal offset of the foot from under the hip and its height above the ground.</returns>
    protected static (double OffsetX, double Lift) GetFootOffset(double phase, Gait gait)
    {
        var progress = (phase % TwoPi) / TwoPi;
        if (progress < gait.StanceFraction)
        {
            // Planted: the foot moves back at a constant speed, matching the ground.
            return (gait.Reach * (1 - (2 * progress / gait.StanceFraction)), 0);
        }

        // Swinging forward through the air.
        var swing = (progress - gait.StanceFraction) / (1 - gait.StanceFraction);
        return (-gait.Reach * Math.Cos(Math.PI * swing), Math.Sin(Math.PI * swing) * gait.Lift);
    }

    /// <summary>
    /// Leg motion of a gait.
    /// </summary>
    /// <param name="Reach">Distance the foot moves along the body from under the hip.</param>
    /// <param name="Lift">Height the foot is lifted to while swinging.</param>
    /// <param name="StanceFraction">Fraction of the cycle the foot stays on the ground.</param>
    protected readonly record struct Gait(double Reach, double Lift, double StanceFraction);
}
