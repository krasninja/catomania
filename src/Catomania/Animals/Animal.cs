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

    // The highest point of the hop, the tallest animal still fits into the design space.
    private const double HopHeight = 14;
    private const double SpinHeight = 16;
    private const double BunnyHopHeight = 8;

    private const double WiggleRocks = 4;
    private const double WiggleAngle = 8 * Math.PI / 180;
    private const double ShakeJiggles = 7;
    private const double ShakeSkew = 0.2;

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
            AnimalKind.Dog => new Dog(settings.DogCoat),
            AnimalKind.Snake => new Snake(),
            AnimalKind.Parrot => new Parrot(settings.ParrotPlumage),
            _ => new Cat(settings.ColorScheme),
        };
    }

    /// <summary>
    /// Create the simulation options that define how the animal moves.
    /// </summary>
    /// <returns>Simulation options.</returns>
    public virtual AnimalSimulatorOptions CreateSimulatorOptions() => new();

    /// <summary>
    /// Draw a frame of the animal in the design space, facing right. The tricks that move the whole
    /// body, like hops and wiggles, are applied here for all animals.
    /// </summary>
    /// <param name="context">Drawing context.</param>
    /// <param name="state">Render state produced by the simulation.</param>
    public void Render(DrawingContext context, AnimalRenderState state)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (GetTrickTransform(state) is not { } transform)
        {
            Draw(context, state);
            return;
        }

        using (context.PushTransform(transform))
        {
            Draw(context, state);
        }
    }

    /// <summary>
    /// Draw a frame of the animal in the design space, facing right.
    /// </summary>
    /// <param name="context">Drawing context.</param>
    /// <param name="state">Render state produced by the simulation.</param>
    protected abstract void Draw(DrawingContext context, AnimalRenderState state);

    /// <summary>
    /// Draw the shadow on the ground. It stays on the ground during the tricks and shrinks while the animal hops.
    /// </summary>
    /// <param name="context">Drawing context.</param>
    /// <param name="brush">Shadow brush.</param>
    /// <param name="state">Render state produced by the simulation.</param>
    /// <param name="centerX">Horizontal center of the shadow.</param>
    /// <param name="radiusX">Horizontal radius.</param>
    /// <param name="radiusY">Vertical radius.</param>
    protected static void DrawShadow(
        DrawingContext context,
        IBrush brush,
        AnimalRenderState state,
        double centerX,
        double radiusX,
        double radiusY)
    {
        ArgumentNullException.ThrowIfNull(context);
        var center = new Point(centerX, GroundY + 2);
        if (GetTrickTransform(state) is not { } transform || !transform.TryInvert(out var inverse))
        {
            context.DrawEllipse(brush, null, center, radiusX, radiusY);
            return;
        }

        // Undo the trick transform pushed by Render.
        var shrink = 1 - (0.35 * GetHopHeight(state) / HopHeight);
        using (context.PushTransform(inverse))
        {
            context.DrawEllipse(brush, null, center, radiusX * shrink, radiusY * shrink);
        }
    }

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

    private static Matrix? GetTrickTransform(AnimalRenderState state)
    {
        var progress = Math.Clamp(state.TrickProgress, 0, 1);

        // Rises and fades out smoothly, so the trick does not jerk at the start and at the end.
        var envelope = Math.Sin(Math.PI * progress);
        return state.Trick switch
        {
            AnimalTrick.Hop or AnimalTrick.Spin or AnimalTrick.BunnyHops => GetHopTransform(state),
            AnimalTrick.Wiggle => AroundFeet(Matrix.CreateRotation(
                Math.Sin(progress * TwoPi * WiggleRocks) * WiggleAngle * envelope)),
            AnimalTrick.Shake => AroundFeet(Matrix.CreateSkew(
                Math.Sin(progress * TwoPi * ShakeJiggles) * ShakeSkew * envelope, 0)),
            _ => null,
        };
    }

    private static Matrix GetHopTransform(AnimalRenderState state)
    {
        var (height, scaleX, scaleY) = GetHopPose(state);
        return AroundFeet(Matrix.CreateScale(scaleX, scaleY)) * Matrix.CreateTranslation(0, -height);
    }

    // Applies the transform with the point on the ground between the feet as the origin.
    private static Matrix AroundFeet(Matrix transform) => Matrix.CreateTranslation(-DesignSize / 2, -GroundY)
        * transform
        * Matrix.CreateTranslation(DesignSize / 2, GroundY);

    private static double GetHopHeight(AnimalRenderState state) => state.Trick is AnimalTrick.Hop
        or AnimalTrick.Spin
        or AnimalTrick.BunnyHops
            ? GetHopPose(state).Height
            : 0;

    private static (double Height, double ScaleX, double ScaleY) GetHopPose(AnimalRenderState state)
    {
        const double crouchFraction = AnimalSimulator.HopCrouchFraction;
        var progress = Math.Clamp(state.TrickProgress, 0, 1);
        var maxHeight = HopHeight;
        if (state.Trick == AnimalTrick.BunnyHops)
        {
            // A series of small hops, each one goes through the full crouch, flight and landing.
            var hops = progress * AnimalSimulator.BunnyHopCount;
            progress = Math.Min(1, hops - Math.Min(Math.Floor(hops), AnimalSimulator.BunnyHopCount - 1));
            maxHeight = BunnyHopHeight;
        }
        else if (state.Trick == AnimalTrick.Spin)
        {
            maxHeight = SpinHeight;
        }

        var crouch = progress < crouchFraction
            ? progress / crouchFraction
            : progress > 1 - crouchFraction
                ? (1 - progress) / crouchFraction
                : -1;
        if (crouch >= 0)
        {
            // Squashed against the ground before the take-off and after the landing.
            var squash = Math.Sin(Math.PI * crouch);
            return (0, 1 + (0.06 * squash), 1 - (0.1 * squash));
        }

        // In the air, stretched when going up and coming down.
        var flight = (progress - crouchFraction) / (1 - (2 * crouchFraction));
        var stretch = Math.Abs(Math.Cos(Math.PI * flight));
        var scaleX = 1 - (0.04 * stretch);
        if (state.Trick == AnimalTrick.Spin)
        {
            // Turning around: the body narrows to the side view of a flat sprite, flips and widens back.
            // It never gets exactly flat, so the transform stays invertible for the shadow.
            var turn = Math.Cos(TwoPi * flight);
            scaleX *= Math.Abs(turn) < 0.05 ? Math.CopySign(0.05, turn) : turn;
        }

        return (Math.Sin(Math.PI * flight) * maxHeight, scaleX, 1 + (0.06 * stretch));
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
