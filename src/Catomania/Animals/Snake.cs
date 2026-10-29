using Avalonia;
using Avalonia.Media;
using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Animals;

/// <summary>
/// A short, chubby mint snake with a big head and a leaf sprouting on top. It scoots like an inchworm,
/// arching the body up and pulling the tail in, and sleeps curled into a ring with the head on it.
/// </summary>
public sealed class Snake : Animal
{
    // A snake has no feet to plant, the strides only set how quickly it scoots at a given speed.
    private const double WalkStride = 36;
    private const double RunStride = 70;

    private const double TailX = 18;
    private const double NeckX = 60;
    private const double BodyWidth = 18;
    private const double TailWidth = 6;
    private const double HeadRadiusX = 17;
    private const double HeadRadiusY = 15;
    private const double SleepingHeadRadiusX = 16;
    private const double SleepingHeadRadiusY = 14;
    private const double OutlineWidth = 1.4;
    private const double RingWidth = 16;
    private const int BodySegments = 24;
    private const int NeckSegments = 7;
    private const int DanglingSegments = 20;

    private static readonly Color SkinColor = Color.FromUInt32(0xFF6DD3A0);
    private static readonly Color IrisColor = Color.FromUInt32(0xFF5C3A21);

    private static readonly IBrush Skin = new SolidColorBrush(SkinColor);
    private static readonly IBrush Spot = new SolidColorBrush(Color.FromUInt32(0xFF4DB487));
    private static readonly IBrush Belly = new SolidColorBrush(Color.FromUInt32(0xFFFFF0C2));
    private static readonly IBrush Leaf = new SolidColorBrush(Color.FromUInt32(0xFF9BE15D));
    private static readonly IBrush Iris = new SolidColorBrush(IrisColor);
    private static readonly IBrush Blush = new SolidColorBrush(Color.FromUInt32(0xCCF48CA0));
    private static readonly IBrush OpenMouth = new SolidColorBrush(Color.FromUInt32(0xFF3B1F24));
    private static readonly IBrush ShadowBrush = new SolidColorBrush(Color.FromUInt32(0x28000000));
    private static readonly IBrush OutlineBrush = new SolidColorBrush(Color.FromUInt32(0xFF1E4D3A));
    private static readonly IBrush LeafDark = new SolidColorBrush(Color.FromUInt32(0xFF2E5C1E));
    private static readonly IBrush TongueBrush = new SolidColorBrush(Color.FromUInt32(0xFFE5484D));
    private static readonly IPen OutlinePen = new Pen(OutlineBrush, OutlineWidth, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen EyeOutlinePen = new Pen(OutlineBrush, 0.8);
    private static readonly IPen ClosedEyePen = new Pen(OutlineBrush, 1.3, null, PenLineCap.Round);
    private static readonly IPen MouthPen = new Pen(OutlineBrush, 1.1, null, PenLineCap.Round);
    private static readonly IPen TonguePen = new Pen(TongueBrush, 1.3, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen StemPen = new Pen(LeafDark, 1.4, null, PenLineCap.Round);
    private static readonly IPen LeafPen = new Pen(LeafDark, 1.2, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen VeinPen = new Pen(LeafDark, 0.8, null, PenLineCap.Round);
    private static readonly IPen RingOutlinePen = new Pen(OutlineBrush, RingWidth + (OutlineWidth * 2));
    private static readonly IPen RingPen = new Pen(Skin, RingWidth);
    private static readonly IPen ZzzPen = new Pen(
        new SolidColorBrush(Color.FromUInt32(0xFFC9D6E3)), 1.8, null, PenLineCap.Round, PenLineJoin.Round);

    // The leaf is drawn growing up from its stem base at the origin, leaning to the right.
    private static readonly Geometry LeafStem = StreamGeometry.Parse("M0 2 Q-1 -3 1 -6");
    private static readonly Geometry LeafBlade = StreamGeometry.Parse("M1 -6 C4 -12 11 -12 13 -11 C11 -6 6 -4 1 -6 Z");
    private static readonly Geometry LeafVein = StreamGeometry.Parse("M2.5 -6.6 Q7 -9 11 -10.6");

    private static readonly Dictionary<(IBrush Brush, int QuarterWidth), IPen> TubePens = [];

    // Spots on the back, as indices of the body points.
    private static readonly int[] SpotIndices = [8, 13, 18];

    // Spots on the sleeping ring, as angles in radians.
    private static readonly double[] RingSpotAngles = [3.7, 4.7, 5.6];

    /// <inheritdoc />
    public override AnimalKind Kind => AnimalKind.Snake;

    /// <inheritdoc />
    public override Color BodyColor => SkinColor;

    /// <inheritdoc />
    public override Color EyeColor => IrisColor;

    /// <inheritdoc />
    public override string Sound => "Hss~";

    /// <inheritdoc />
    public override AnimalSimulatorOptions CreateSimulatorOptions() => new()
    {
        WalkStrideLength = WalkStride / DesignSize,
        RunStrideLength = RunStride / DesignSize,
    };

    /// <inheritdoc />
    public override void Render(DrawingContext context, AnimalRenderState state)
    {
        switch (state.State)
        {
            case AnimalAnimationState.Sleep:
                DrawSleeping(context, state.Phase);
                break;
            case AnimalAnimationState.Dragged:
                DrawDangling(context, state.Phase);
                break;
            default:
                DrawCrawling(context, state);
                break;
        }
    }

    private static void DrawCrawling(DrawingContext context, AnimalRenderState state)
    {
        var phase = state.Phase;
        var isIdle = state.State == AnimalAnimationState.Idle;

        // The body arches up once per cycle, pulling the tail in, then stretches flat again.
        var arch = state.State switch
        {
            AnimalAnimationState.Run => 5.5 * (1 - Math.Cos(phase)),
            AnimalAnimationState.Walk => 3.5 * (1 - Math.Cos(phase)),
            _ => 0.6 * (1 + Math.Sin(phase * 0.5)),
        };
        var tailX = TailX + (arch * 0.9);
        var bob = (-arch * 0.25) + (isIdle ? Math.Sin(phase * 0.5) * 0.8 : 0);

        context.DrawEllipse(ShadowBrush, null, new Point(50, GroundY + 2), 30 - (arch * 0.3), 4);

        var points = new Point[BodySegments + 1 + NeckSegments];
        var widths = new double[points.Length];
        for (var i = 0; i <= BodySegments; i++)
        {
            var t = (double)i / BodySegments;
            var width = TailWidth + ((BodyWidth - TailWidth) * Math.Sin(Math.Min(1, t * 1.5) * Math.PI / 2));
            // Flat before the neck, so the back does not fold where the neck rises.
            var lift = Math.Sin(Math.PI * Math.Min(1, t / 0.8));
            var y = GroundY - (width / 2) - (arch * lift * lift);
            if (t < 0.14)
            {
                // The tail tip curls up.
                y -= (0.14 - t) * 32;
            }

            points[i] = new Point(tailX + ((NeckX - tailX) * t), y);
            widths[i] = width;
        }

        // The neck rises from the ground to the head.
        for (var i = 1; i <= NeckSegments; i++)
        {
            points[BodySegments + i] = GetBezierPoint(
                new Point(NeckX, GroundY - 9), new Point(66, GroundY - 9), new Point(68, 78), new Point(69, 69 + bob),
                (double)i / NeckSegments);
            widths[BodySegments + i] = BodyWidth;
        }

        DrawBody(context, points, widths, 4);

        var leafSway = Math.Sin(isIdle ? phase : phase * 2) * (state.State == AnimalAnimationState.Run ? 0.35 : 0.18);
        DrawLeaf(context, new Point(68, 39 + bob), -0.15 + leafSway);

        // Flicks the tongue once per phase period while idle.
        var tongue = isIdle && phase < 1 ? Math.Sin(Math.PI * phase) : 0;
        DrawHead(
            context,
            new Point(72, 53 + bob),
            HeadRadiusX,
            HeadRadiusY,
            0,
            state.IsBlinking ? HeadPose.Blinking : HeadPose.Awake,
            tongue);
    }

    private static void DrawSleeping(DrawingContext context, double phase)
    {
        var breathe = Math.Sin(phase);

        context.DrawEllipse(ShadowBrush, null, new Point(50, GroundY + 2), 30, 4);

        // The body, curled into a ring, swells with the breath while lying on the ground.
        var breath = Matrix.CreateTranslation(-50, -GroundY)
            * Matrix.CreateScale(1 + (breathe * 0.025), 1 + (breathe * 0.035))
            * Matrix.CreateTranslation(50, GroundY);
        using (context.PushTransform(breath))
        {
            var ringCenter = new Point(48, 81);
            context.DrawEllipse(null, RingOutlinePen, ringCenter, 22, 6.5);
            context.DrawEllipse(null, RingPen, ringCenter, 22, 6.5);
            foreach (var angle in RingSpotAngles)
            {
                var spot = new Point(ringCenter.X + (Math.Cos(angle) * 22), ringCenter.Y + (Math.Sin(angle) * 6.5) - 3);
                context.DrawEllipse(Spot, null, spot, 2.6, 2.1);
            }

            // The tail coils on top of the ring.
            var tail = new Point[9];
            var tailWidths = new double[tail.Length];
            for (var i = 0; i < tail.Length; i++)
            {
                var t = (double)i / (tail.Length - 1);
                tail[i] = GetBezierPoint(new Point(29, 80), new Point(31, 71), new Point(42, 68), new Point(50, 72), t);
                tailWidths[i] = 13 - (8 * t);
            }

            DrawTube(context, tail, tailWidths, Skin, true);

            var neck = new Point[6];
            var neckWidths = new double[neck.Length];
            for (var i = 0; i < neck.Length; i++)
            {
                neck[i] = GetBezierPoint(
                    new Point(52, 84), new Point(56, 82), new Point(59, 79), new Point(62, 76), (double)i / (neck.Length - 1));
                neckWidths[i] = RingWidth;
            }

            DrawTube(context, neck, neckWidths, Skin, true);
        }

        // The head rests on the ring, the leaf droops over it.
        DrawHead(
            context,
            new Point(64, 71 + (breathe * 0.6)),
            SleepingHeadRadiusX,
            SleepingHeadRadiusY,
            0.14 + (breathe * 0.03),
            HeadPose.Asleep,
            0);
        DrawLeaf(context, new Point(63, 58.5 + (breathe * 0.6)), -1 + (breathe * 0.08), true);

        DrawZzz(context, ZzzPen, new Point(76, 52), phase);
    }

    private static void DrawDangling(DrawingContext context, double phase)
    {
        // Held by the head, the body hangs down and swings, the tail tip kicks forward.
        var points = new Point[DanglingSegments + 1];
        var widths = new double[points.Length];
        for (var i = 0; i <= DanglingSegments; i++)
        {
            var t = (double)i / DanglingSegments;
            var kick = Math.Pow(t, 6) * 9;

            // Ordered from the tail up, the way the crawling body is.
            var index = DanglingSegments - i;
            points[index] = new Point(54 + (Math.Sin(phase - (t * 2)) * t * 5) + kick, 40 + (t * 46));
            widths[index] = BodyWidth - (11 * t * t);
        }

        DrawBody(context, points, widths, 0);

        // The leaf perks up and wobbles.
        DrawLeaf(context, new Point(53, 13), -0.4 + (Math.Sin(phase * 2) * 0.25));
        DrawHead(
            context,
            new Point(57, 28),
            HeadRadiusX,
            HeadRadiusY,
            -0.18 + (Math.Sin(phase) * 0.05),
            HeadPose.Surprised,
            0);
    }

    private static void DrawBody(DrawingContext context, Point[] points, double[] widths, int bellyStart)
    {
        DrawTube(context, points, widths, Skin, true);

        // A pale belly stripe on the underside and spots along the back.
        var bellyPoints = new Point[points.Length - bellyStart];
        var bellyWidths = new double[bellyPoints.Length];
        for (var i = bellyStart; i < points.Length; i++)
        {
            bellyPoints[i - bellyStart] = points[i] + (GetBellyNormal(points, i) * widths[i] * 0.24);
            bellyWidths[i - bellyStart] = widths[i] * 0.36;
        }

        DrawTube(context, bellyPoints, bellyWidths, Belly, false);

        foreach (var index in SpotIndices)
        {
            var center = points[index] - (GetBellyNormal(points, index) * widths[index] * 0.2);
            context.DrawEllipse(Spot, null, center, widths[index] * 0.15, widths[index] * 0.13);
        }
    }

    private static void DrawLeaf(DrawingContext context, Point stemBase, double angle, bool isMirrored = false)
    {
        var mirror = isMirrored ? Matrix.CreateScale(-1, 1) : Matrix.Identity;
        using (context.PushTransform(mirror * Matrix.CreateRotation(angle) * Matrix.CreateTranslation(stemBase.X, stemBase.Y)))
        {
            context.DrawGeometry(null, StemPen, LeafStem);
            context.DrawGeometry(Leaf, LeafPen, LeafBlade);
            context.DrawGeometry(null, VeinPen, LeafVein);
        }
    }

    private static void DrawHead(
        DrawingContext context,
        Point center,
        double radiusX,
        double radiusY,
        double angle,
        HeadPose pose,
        double tongue)
    {
        using (context.PushTransform(Matrix.CreateRotation(angle) * Matrix.CreateTranslation(center.X, center.Y)))
        {
            context.DrawEllipse(Skin, null, default, radiusX, radiusY);
            using (context.PushGeometryClip(new EllipseGeometry(new Rect(-radiusX, -radiusY, radiusX * 2, radiusY * 2))))
            {
                // Pale chin.
                context.DrawEllipse(Belly, null, new Point(radiusX * 0.3, radiusY * 0.78), radiusX * 0.72, radiusY * 0.42);
            }

            context.DrawEllipse(null, OutlinePen, default, radiusX, radiusY);

            var eyeRadius = radiusY * 0.3;
            DrawEye(context, new Point(-radiusX * 0.14, -radiusY * 0.12), eyeRadius, pose);
            DrawEye(context, new Point(radiusX * 0.42, -radiusY * 0.16), eyeRadius, pose);

            context.DrawEllipse(Blush, null, new Point(-radiusX * 0.32, radiusY * 0.34), radiusX * 0.16, radiusY * 0.1);
            context.DrawEllipse(Blush, null, new Point(radiusX * 0.64, radiusY * 0.3), radiusX * 0.14, radiusY * 0.1);

            // Nostril.
            context.DrawEllipse(OutlineBrush, null, new Point(radiusX * 0.86, -radiusY * 0.02), 0.6, 0.5);

            if (pose == HeadPose.Surprised)
            {
                context.DrawEllipse(OpenMouth, null, new Point(radiusX * 0.5, radiusY * 0.46), 1.9, 1.5);
            }
            else
            {
                // A narrower smile while asleep.
                var halfWidth = pose == HeadPose.Asleep ? 0.22 : 0.32;
                var depth = pose == HeadPose.Asleep ? 0.18 : 0.26;
                var mouth = new StreamGeometry();
                using (var geometryContext = mouth.Open())
                {
                    geometryContext.BeginFigure(new Point(radiusX * (0.48 - halfWidth), radiusY * 0.36), false);
                    geometryContext.QuadraticBezierTo(
                        new Point(radiusX * 0.48, radiusY * (0.36 + depth)),
                        new Point(radiusX * (0.48 + halfWidth), radiusY * 0.32));
                    geometryContext.EndFigure(false);
                }

                context.DrawGeometry(null, MouthPen, mouth);
            }

            if (tongue > 0.02)
            {
                DrawTongue(context, new Point(radiusX * 0.8, radiusY * 0.4), tongue);
            }
        }
    }

    private static void DrawEye(DrawingContext context, Point center, double radius, HeadPose pose)
    {
        if (pose is HeadPose.Blinking or HeadPose.Asleep)
        {
            var closedEye = new StreamGeometry();
            using (var geometryContext = closedEye.Open())
            {
                geometryContext.BeginFigure(new Point(center.X - (radius * 0.9), center.Y), false);
                geometryContext.QuadraticBezierTo(
                    new Point(center.X, center.Y + (radius * 0.9)), new Point(center.X + (radius * 0.9), center.Y));
                geometryContext.EndFigure(false);
            }

            context.DrawGeometry(null, ClosedEyePen, closedEye);
            return;
        }

        // Wide open and with small irises when the snake is lifted.
        var isSurprised = pose == HeadPose.Surprised;
        var eyeRadius = isSurprised ? radius * 1.12 : radius;
        var irisRadius = isSurprised ? eyeRadius * 0.55 : eyeRadius * 0.78;
        context.DrawEllipse(Brushes.White, EyeOutlinePen, center, eyeRadius, eyeRadius);
        context.DrawEllipse(
            Iris, null, new Point(center.X + (eyeRadius * 0.14), center.Y + (eyeRadius * 0.06)), irisRadius, irisRadius);
        context.DrawEllipse(
            Brushes.White,
            null,
            new Point(center.X - (eyeRadius * 0.22), center.Y - (eyeRadius * 0.32)),
            eyeRadius * 0.32,
            eyeRadius * 0.32);
        context.DrawEllipse(
            Brushes.White,
            null,
            new Point(center.X + (eyeRadius * 0.34), center.Y + (eyeRadius * 0.32)),
            eyeRadius * 0.13,
            eyeRadius * 0.13);
    }

    private static void DrawTongue(DrawingContext context, Point root, double extension)
    {
        // A forked tongue sticking out of the mouth corner.
        var length = 7 * extension;
        var tip = new Point(root.X + length, root.Y + (length * 0.15));
        var geometry = new StreamGeometry();
        using (var geometryContext = geometry.Open())
        {
            geometryContext.BeginFigure(root, false);
            geometryContext.LineTo(tip);
            geometryContext.EndFigure(false);
            geometryContext.BeginFigure(new Point(tip.X + (2.2 * extension), tip.Y - (1.4 * extension)), false);
            geometryContext.LineTo(tip);
            geometryContext.LineTo(new Point(tip.X + (2.2 * extension), tip.Y + (1.6 * extension)));
            geometryContext.EndFigure(false);
        }

        context.DrawGeometry(null, TonguePen, geometry);
    }

    /// <summary>
    /// Draw a tube of varying width along a polyline as round capped segments. Unlike an outline polygon,
    /// the strokes do not fold where the fat body bends sharply.
    /// </summary>
    /// <param name="context">Drawing context.</param>
    /// <param name="points">Points along the middle of the tube.</param>
    /// <param name="widths">Width of the tube at every point.</param>
    /// <param name="brush">Fill of the tube.</param>
    /// <param name="isOutlined">Whether to outline the tube.</param>
    private static void DrawTube(DrawingContext context, Point[] points, double[] widths, IBrush brush, bool isOutlined)
    {
        // The whole outline goes first, so the fill covers it at the joints.
        if (isOutlined)
        {
            for (var i = 1; i < points.Length; i++)
            {
                var width = ((widths[i - 1] + widths[i]) / 2) + (OutlineWidth * 2);
                context.DrawLine(GetTubePen(OutlineBrush, width), points[i - 1], points[i]);
            }
        }

        for (var i = 1; i < points.Length; i++)
        {
            context.DrawLine(GetTubePen(brush, (widths[i - 1] + widths[i]) / 2), points[i - 1], points[i]);
        }
    }

    private static IPen GetTubePen(IBrush brush, double width)
    {
        // Widths are rounded to a quarter of a unit, so a few dozen pens serve every frame.
        var key = (Brush: brush, QuarterWidth: (int)Math.Round(width * 4));
        if (!TubePens.TryGetValue(key, out var pen))
        {
            pen = new Pen(brush, key.QuarterWidth / 4.0, null, PenLineCap.Round, PenLineJoin.Round);
            TubePens[key] = pen;
        }

        return pen;
    }

    /// <summary>
    /// Get the unit normal on the belly side of a body going from the tail to the head: down for a body crawling
    /// to the right, forward for a raised neck.
    /// </summary>
    /// <param name="points">Points along the body, from the tail to the head.</param>
    /// <param name="index">Index of the point.</param>
    /// <returns>Unit normal.</returns>
    private static Vector GetBellyNormal(Point[] points, int index)
    {
        Vector direction = points[Math.Min(points.Length - 1, index + 1)] - points[Math.Max(0, index - 1)];
        var length = direction.Length;
        return length > 0 ? new Vector(-direction.Y / length, direction.X / length) : new Vector(0, 1);
    }

    private static Point GetBezierPoint(Point start, Point controlPoint1, Point controlPoint2, Point end, double t)
    {
        var u = 1 - t;
        return (start * (u * u * u)) + (controlPoint1 * (3 * u * u * t)) + (controlPoint2 * (3 * u * t * t)) + (end * (t * t * t));
    }

    private enum HeadPose
    {
        Awake,
        Blinking,
        Asleep,
        Surprised,
    }
}
