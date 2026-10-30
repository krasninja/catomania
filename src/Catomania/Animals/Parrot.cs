using Avalonia;
using Avalonia.Media;
using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Animals;

/// <summary>
/// A scarlet macaw drawn in profile: red body, wings banded yellow, green and blue, a long tail with a blue tip,
/// a white face and a big hooked beak. It waddles, runs in hops flapping its wings, sleeps squatting with
/// the head tucked into the chest and flaps both wings when it is picked up.
/// </summary>
public sealed class Parrot : Animal
{
    // Short legs take short steps, see Cat for how the stride is chosen. The run is a hop per cycle.
    private const double WalkStanceFraction = 0.5;
    private const double WalkReach = 5;
    private const double WalkLift = 3;
    private const double WalkStride = 2 * WalkReach / WalkStanceFraction;
    private const double RunStride = 44;
    private const double HopHeight = 7;

    private const double DanglingLegLength = 12;

    private static readonly Color PlumageColor = Color.FromUInt32(0xFFE0342F);
    private static readonly Color IrisColor = Color.FromUInt32(0xFFFAE278);

    private static readonly IBrush Plumage = new SolidColorBrush(PlumageColor);
    private static readonly IBrush Yellow = new SolidColorBrush(Color.FromUInt32(0xFFFFC83D));
    private static readonly IBrush Green = new SolidColorBrush(Color.FromUInt32(0xFF3FAE5A));
    private static readonly IBrush Blue = new SolidColorBrush(Color.FromUInt32(0xFF2F7BD8));
    private static readonly IBrush Face = new SolidColorBrush(Color.FromUInt32(0xFFFFF6EC));
    private static readonly IBrush UpperBeakBrush = new SolidColorBrush(Color.FromUInt32(0xFFF0E2C8));
    private static readonly IBrush LowerBeakBrush = new SolidColorBrush(Color.FromUInt32(0xFF2E2A2A));
    private static readonly IBrush Iris = new SolidColorBrush(IrisColor);
    private static readonly IBrush Pupil = new SolidColorBrush(Color.FromUInt32(0xFF140C08));
    private static readonly IBrush Blush = new SolidColorBrush(Color.FromUInt32(0xAAF48CA0));
    private static readonly IBrush BodyShade = new SolidColorBrush(Color.FromUInt32(0x30000000));
    private static readonly IBrush FarWingShade = new SolidColorBrush(Color.FromUInt32(0x55000000));
    private static readonly IBrush ShadowBrush = new SolidColorBrush(Color.FromUInt32(0x28000000));
    private static readonly IBrush OutlineBrush = new SolidColorBrush(Color.FromUInt32(0xFF5A1414));
    private static readonly IBrush BeakOutlineBrush = new SolidColorBrush(Color.FromUInt32(0xFF2B2B2B));
    private static readonly IBrush FaceLineBrush = new SolidColorBrush(Color.FromUInt32(0xFFE8A0A0));
    private static readonly IBrush FeatherBrush = new SolidColorBrush(Color.FromUInt32(0x40000000));
    private static readonly IBrush LegBrush = new SolidColorBrush(Color.FromUInt32(0xFF6B6B73));
    private static readonly IPen OutlinePen = new Pen(OutlineBrush, 1.3, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen BeakOutlinePen = new Pen(BeakOutlineBrush, 1.1, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen EyeRingPen = new Pen(BeakOutlineBrush, 0.7);
    private static readonly IPen ClosedEyePen = new Pen(BeakOutlineBrush, 1.3, null, PenLineCap.Round);
    private static readonly IPen FaceLinePen = new Pen(FaceLineBrush, 0.8, null, PenLineCap.Round);
    private static readonly IPen FeatherPen = new Pen(FeatherBrush, 0.8, null, PenLineCap.Round);
    private static readonly IPen LegPen = new Pen(LegBrush, 3, null, PenLineCap.Round);
    private static readonly IPen ToePen = new Pen(LegBrush, 2, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen ZzzPen = new Pen(
        new SolidColorBrush(Color.FromUInt32(0xFFC9D6E3)), 1.8, null, PenLineCap.Round, PenLineJoin.Round);

    private static readonly Geometry Body = Parse(
        "M44 46 C56 42 66 52 64 66 C62 78 54 84 45 83 C36 82 32 74 34 64 C35 56 38 49 44 46 Z");

    private static readonly Geometry BellyShade = Parse("M36 72 C40 80 50 84 58 80 C54 84 48 84 44 83 C38 82 35 78 36 72 Z");

    // The tail is drawn growing down-left from its base at the origin.
    private static readonly Geometry Tail = Parse(
        "M4 -5 C-4 0 -16 8 -30 14 C-32 15.5 -30 17 -27 16.5 C-14 13 -2 7 6 2 Z");

    // The wing is drawn hanging down-back from the shoulder at the origin.
    private static readonly Geometry Wing = Parse(
        "M3 -3 C10 3 9 18 3 28 C-1 33 -8 37 -15 39 C-12 30 -10 18 -8 8 C-6 1 -2 -4 3 -3 Z");

    // The beak is drawn from where it meets the face at the origin.
    private static readonly Geometry UpperBeak = Parse("M0 -1 C7 -3 13 3 12 12 C11.5 15 9.5 15 9 12.5 C8 8 5 6.5 1 7 Z");

    private static readonly Geometry LowerBeak = Parse("M1 6.5 C4 6 7.5 8.5 8 11.5 C6 13.5 2.5 13 0.5 10 Z");

    private static readonly Point TailBase = new(38, 76);
    private static readonly Point NearShoulder = new(50, 51);
    private static readonly Point FarShoulder = new(46, 49);
    private static readonly Point HeadCenter = new(60, 38);
    private static readonly Point[] Hips = [new(43, 80), new(50, 80)];

    /// <inheritdoc />
    public override AnimalKind Kind => AnimalKind.Parrot;

    /// <inheritdoc />
    public override Color BodyColor => PlumageColor;

    /// <inheritdoc />
    public override Color EyeColor => IrisColor;

    /// <inheritdoc />
    public override string Sound => "Squawk!";

    /// <inheritdoc />
    public override AnimalSimulatorOptions CreateSimulatorOptions() => new()
    {
        WalkStrideLength = WalkStride / DesignSize,
        RunStrideLength = RunStride / DesignSize,
    };

    /// <inheritdoc />
    public override void Render(DrawingContext context, AnimalRenderState state)
    {
        if (state.State == AnimalAnimationState.Sleep)
        {
            DrawSleeping(context, state.Phase);
        }
        else
        {
            DrawStanding(context, state);
        }
    }

    private static void DrawStanding(DrawingContext context, AnimalRenderState state)
    {
        var phase = state.Phase;
        var pose = state.State switch
        {
            // Hops once per cycle, flapping twice: on the ground for the second half of the cycle.
            AnimalAnimationState.Run => new Pose(
                Math.Max(0, Math.Sin(phase)) * HopHeight, 14, 95 + (35 * Math.Sin(phase * 2)), 6, -6),
            // Waddles, rocking from foot to foot.
            AnimalAnimationState.Walk => new Pose(
                Math.Abs(Math.Sin(phase)) * 1.2, Math.Sin(phase) * 4, 0, 0, Math.Sin(phase) * 4),
            // Held up in the air, flapping both wings.
            AnimalAnimationState.Dragged => new Pose(
                5, Math.Sin(phase) * 4, 105 + (25 * Math.Sin(phase * 3)), -10, Math.Sin(phase) * 5),
            // Tilts the head from side to side, curious.
            _ => new Pose(0, 0, 0, Math.Sin(phase * 0.5) * 10, Math.Sin(phase) * 2),
        };
        var isFlapping = state.State is AnimalAnimationState.Run or AnimalAnimationState.Dragged;

        context.DrawEllipse(ShadowBrush, null, new Point(46, GroundY + 2), 20 - (pose.Lift * 0.6), 4);

        using (context.PushTransform(Matrix.CreateTranslation(0, -pose.Lift)))
        {
            for (var i = 0; i < Hips.Length; i++)
            {
                DrawLeg(context, state, Hips[i], phase + (i * Math.PI), pose.Lift);
            }

            using (context.PushTransform(RotateAround(pose.Tilt, 47, 80)))
            {
                if (isFlapping)
                {
                    DrawWing(context, FarShoulder, pose.WingAngle + 15, isFar: true);
                }

                using (context.PushTransform(Matrix.CreateRotation(ToRadians(pose.TailSway)) * Matrix.CreateTranslation(TailBase.X, TailBase.Y)))
                {
                    DrawTail(context);
                }

                context.DrawGeometry(Plumage, OutlinePen, Body);
                context.DrawGeometry(BodyShade, null, BellyShade);
                DrawWing(context, NearShoulder, pose.WingAngle, isFar: false);

                var eye = state.State switch
                {
                    AnimalAnimationState.Dragged => EyePose.Surprised,
                    _ when state.IsBlinking => EyePose.Closed,
                    _ => EyePose.Open,
                };
                DrawHead(context, HeadCenter, pose.HeadTilt, eye);
            }
        }
    }

    private static void DrawLeg(DrawingContext context, AnimalRenderState state, Point hip, double phase, double lift)
    {
        Point foot;
        var isCurled = false;
        switch (state.State)
        {
            case AnimalAnimationState.Walk:
                var (offsetX, footLift) = GetFootOffset(phase, new Gait(WalkReach, WalkLift, WalkStanceFraction));
                foot = new Point(hip.X + offsetX, GroundY - footLift + lift);
                break;
            case AnimalAnimationState.Run when lift > 0.5:
                // Tucked under the body in the air.
                foot = new Point(hip.X - 4, hip.Y + 6);
                isCurled = true;
                break;
            case AnimalAnimationState.Dragged:
                foot = new Point(hip.X + 1 + (Math.Sin(state.Phase) * 1.5), hip.Y + DanglingLegLength);
                isCurled = true;
                break;
            default:
                foot = new Point(hip.X, GroundY + lift);
                break;
        }

        context.DrawLine(LegPen, hip, foot);
        if (isCurled)
        {
            context.DrawGeometry(null, ToePen, CreateCurve(
                foot, new Point(foot.X + 3, foot.Y + 1), new Point(foot.X + 4, foot.Y + 3), new Point(foot.X + 2, foot.Y + 4)));
            context.DrawGeometry(null, ToePen, CreateCurve(
                foot, new Point(foot.X - 2, foot.Y + 1), new Point(foot.X - 3, foot.Y + 3), new Point(foot.X - 1, foot.Y + 4)));
            return;
        }

        // Parrot toes: two point forward and one back.
        context.DrawLine(ToePen, foot, new Point(foot.X + 4.5, foot.Y));
        context.DrawLine(ToePen, foot, new Point(foot.X + 3.5, foot.Y - 1.2));
        context.DrawLine(ToePen, foot, new Point(foot.X - 3, foot.Y));
    }

    private static void DrawSleeping(DrawingContext context, double phase)
    {
        var breathe = Math.Sin(phase);

        context.DrawEllipse(ShadowBrush, null, new Point(46, GroundY + 2), 22, 4);

        // Squatting with the feathers fluffed up, the body swells with the breath while sitting on the ground.
        var breath = Matrix.CreateTranslation(-47, -GroundY)
            * Matrix.CreateScale(1.12 + (breathe * 0.02), 0.88 + (breathe * 0.025))
            * Matrix.CreateTranslation(47, GroundY + 9);
        using (context.PushTransform(breath))
        {
            using (context.PushTransform(Matrix.CreateScale(0.8, 0.8) * Matrix.CreateRotation(ToRadians(12)) * Matrix.CreateTranslation(36, 74)))
            {
                DrawTail(context);
            }

            context.DrawGeometry(Plumage, OutlinePen, Body);
            context.DrawGeometry(BodyShade, null, BellyShade);

            // The wing is folded higher up the back, so it does not reach the ground.
            using (context.PushTransform(Matrix.CreateScale(1, 0.82) * Matrix.CreateTranslation(0, 9)))
            {
                DrawWing(context, NearShoulder, -4, isFar: false);
            }

            DrawHead(context, new Point(57, 44), 38, EyePose.Closed);
        }

        DrawZzz(context, ZzzPen, new Point(68, 34), phase);
    }

    private static void DrawTail(DrawingContext context)
    {
        context.DrawGeometry(Plumage, null, Tail);
        using (context.PushGeometryClip(Tail))
        {
            context.DrawEllipse(Blue, null, new Point(-29, 15), 9, 8);
        }

        context.DrawGeometry(null, OutlinePen, Tail);
    }

    private static void DrawWing(DrawingContext context, Point shoulder, double angleDegrees, bool isFar)
    {
        using (context.PushTransform(Matrix.CreateRotation(ToRadians(angleDegrees)) * Matrix.CreateTranslation(shoulder.X, shoulder.Y)))
        {
            // Blue flight feathers, then green, yellow and red coverts towards the shoulder.
            context.DrawGeometry(Blue, null, Wing);
            using (context.PushGeometryClip(Wing))
            {
                context.FillRectangle(Green, new Rect(-20, 9, 40, 14));
                context.FillRectangle(Yellow, new Rect(-20, 4, 40, 6));
                context.DrawEllipse(Plumage, null, new Point(0, -1), 12, 6);
                context.DrawLine(FeatherPen, new Point(-4, 26), new Point(1, 18));
                context.DrawLine(FeatherPen, new Point(-9, 32), new Point(-3, 22));
                if (isFar)
                {
                    context.FillRectangle(FarWingShade, new Rect(-30, -10, 60, 60));
                }
            }

            context.DrawGeometry(null, OutlinePen, Wing);
        }
    }

    private static void DrawHead(DrawingContext context, Point center, double tiltDegrees, EyePose eye)
    {
        using (context.PushTransform(RotateAround(tiltDegrees, center.X, center.Y)))
        {
            context.DrawEllipse(Plumage, OutlinePen, center, 12.5, 12.5);

            // The bare white face with the rows of tiny red feathers macaws have.
            context.DrawEllipse(Face, null, new Point(center.X + 5, center.Y + 1.5), 7, 8);
            for (var i = 0; i < 3; i++)
            {
                var y = center.Y + 3 + (i * 2.2);
                context.DrawLine(FaceLinePen, new Point(center.X + 1.5 + i, y), new Point(center.X + 4.5 + i, y - 0.6));
            }

            context.DrawEllipse(Blush, null, new Point(center.X + 3, center.Y + 5), 2.4, 1.5);
            DrawEye(context, new Point(center.X + 5.5, center.Y - 2), eye);

            using (context.PushTransform(Matrix.CreateTranslation(center.X + 9.5, center.Y - 4)))
            {
                context.DrawGeometry(LowerBeakBrush, BeakOutlinePen, LowerBeak);
                context.DrawGeometry(UpperBeakBrush, BeakOutlinePen, UpperBeak);
            }
        }
    }

    private static void DrawEye(DrawingContext context, Point center, EyePose eye)
    {
        switch (eye)
        {
            case EyePose.Closed:
                context.DrawGeometry(null, ClosedEyePen, CreateCurve(
                    new Point(center.X - 2.8, center.Y),
                    new Point(center.X - 1.4, center.Y + 2.5),
                    new Point(center.X + 1.4, center.Y + 2.5),
                    new Point(center.X + 2.8, center.Y)));
                return;
            case EyePose.Surprised:
                // Wide open with a small pupil when the parrot is lifted.
                context.DrawEllipse(Iris, EyeRingPen, center, 3.3, 3.5);
                context.DrawEllipse(Pupil, null, center, 1.1, 1.1);
                break;
            default:
                // Macaws have a pale yellow iris around a big pupil.
                context.DrawEllipse(Iris, EyeRingPen, center, 2.8, 3.1);
                context.DrawEllipse(Pupil, null, new Point(center.X + 0.3, center.Y + 0.2), 1.8, 2);
                break;
        }

        context.DrawEllipse(Brushes.White, null, new Point(center.X + 0.9, center.Y - 1.1), 0.9, 0.9);
    }

    private static Matrix RotateAround(double angleDegrees, double x, double y)
        => Matrix.CreateTranslation(-x, -y) * Matrix.CreateRotation(ToRadians(angleDegrees)) * Matrix.CreateTranslation(x, y);

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static Geometry Parse(string pathData) => StreamGeometry.Parse(pathData);

    /// <summary>
    /// Pose of the standing parrot.
    /// </summary>
    /// <param name="Lift">Height of the feet above the ground.</param>
    /// <param name="Tilt">Forward lean of the body in degrees.</param>
    /// <param name="WingAngle">Angle the wings are raised to in degrees, 0 is folded.</param>
    /// <param name="HeadTilt">Tilt of the head in degrees.</param>
    /// <param name="TailSway">Sway of the tail in degrees.</param>
    private readonly record struct Pose(double Lift, double Tilt, double WingAngle, double HeadTilt, double TailSway);

    private enum EyePose
    {
        Open,
        Closed,
        Surprised,
    }
}
