using Avalonia;
using Avalonia.Media;
using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Animals;

/// <summary>
/// A panda cub: a big round head on a small black body with stubby legs, it dozes off sitting up.
/// </summary>
public sealed class Panda : Animal
{
    // The short legs take shorter steps than the cat's, see Cat for how the stride is chosen.
    private const double WalkStanceFraction = 0.5;
    private const double RunStanceFraction = 0.4;
    private const double WalkReach = 5;
    private const double RunReach = 8;
    private const double WalkStride = 2 * WalkReach / WalkStanceFraction;
    private const double RunStride = 2 * RunReach / RunStanceFraction;

    private const double FarHipY = 76;
    private const double NearHipY = 79;
    private const double LegWidth = 9.5;
    private const double DanglingLegLength = 13;

    private static readonly Color FurColor = Color.FromUInt32(0xFFF6F3EC);
    private static readonly Color IrisColor = Color.FromUInt32(0xFF8C5519);

    private static readonly IBrush Fur = new SolidColorBrush(FurColor);
    private static readonly IBrush FurShade = new SolidColorBrush(Color.FromUInt32(0xFFE2DDD2));
    private static readonly IBrush Patch = new SolidColorBrush(Color.FromUInt32(0xFF26252B));
    private static readonly IBrush PatchFar = new SolidColorBrush(Color.FromUInt32(0xFF1A1A1F));
    private static readonly IBrush EarInner = new SolidColorBrush(Color.FromUInt32(0xFF4A4950));
    private static readonly IBrush Iris = new SolidColorBrush(IrisColor);
    private static readonly IBrush Pupil = new SolidColorBrush(Color.FromUInt32(0xFF111114));
    private static readonly IBrush Blush = new SolidColorBrush(Color.FromUInt32(0xBFE9A0A6));
    private static readonly IBrush Drool = new SolidColorBrush(Color.FromUInt32(0xFF9FD3F0));
    private static readonly IBrush ShadowBrush = new SolidColorBrush(Color.FromUInt32(0x28000000));
    private static readonly IPen OutlinePen = new Pen(new SolidColorBrush(Color.FromUInt32(0xFF17161B)), 1.4);
    private static readonly IPen FacePen = new Pen(Pupil, 1.1, null, PenLineCap.Round);
    private static readonly IPen ClosedEyePen = new Pen(Brushes.White, 1.2, null, PenLineCap.Round);
    private static readonly IPen ZzzPen = new Pen(
        new SolidColorBrush(Color.FromUInt32(0xFFC9D6E3)), 1.8, null, PenLineCap.Round, PenLineJoin.Round);

    private static readonly Point[] EyeCenters = [new(63.5, 46.5), new(78, 46.5)];

    // Eye patches are tilted towards the nose.
    private static readonly (Point Center, double Angle)[] Patches = [(new(63, 47), 25), (new(78.5, 47), -25)];

    private static readonly Geometry[] ClosedEyes = Array.ConvertAll(EyeCenters, center => CreateCurve(
        new Point(center.X - 2.8, center.Y), new Point(center.X - 1.2, center.Y + 2.6),
        new Point(center.X + 1.2, center.Y + 2.6), new Point(center.X + 2.8, center.Y)));

    private static readonly Geometry Nose = CreateTriangle(
        new Point(68.8, 52.6), new Point(72.8, 52.6), new Point(70.8, 54.7));

    private static readonly Geometry MouthLeft = CreateCurve(
        new Point(70.8, 54.7), new Point(70.4, 56.6), new Point(69, 57.1), new Point(68, 56.4));

    private static readonly Geometry MouthRight = CreateCurve(
        new Point(70.8, 54.7), new Point(71.2, 56.6), new Point(72.6, 57.1), new Point(73.6, 56.4));

    // The sleeping cub sits facing the viewer, its head is drawn centered and then tilted.
    private static readonly Point[] SleepingEyeCenters = [new(42.8, 53), new(57.2, 53)];

    private static readonly (Point Center, double Angle)[] SleepingPatches = [(new(42.5, 53), 25), (new(57.5, 53), -25)];

    private static readonly Geometry[] SleepingClosedEyes = Array.ConvertAll(SleepingEyeCenters, center => CreateCurve(
        new Point(center.X - 2.8, center.Y), new Point(center.X - 1.2, center.Y + 2.6),
        new Point(center.X + 1.2, center.Y + 2.6), new Point(center.X + 2.8, center.Y)));

    private static readonly Geometry SleepingNose = CreateTriangle(
        new Point(48, 58.6), new Point(52, 58.6), new Point(50, 60.7));

    private static readonly Geometry SleepingMouthLeft = CreateCurve(
        new Point(50, 60.7), new Point(49.6, 62.6), new Point(48.2, 63.1), new Point(47.2, 62.4));

    private static readonly Geometry SleepingMouthRight = CreateCurve(
        new Point(50, 60.7), new Point(50.4, 62.6), new Point(51.8, 63.1), new Point(52.8, 62.4));

    /// <inheritdoc />
    public override AnimalKind Kind => AnimalKind.Panda;

    /// <inheritdoc />
    public override Color BodyColor => FurColor;

    /// <inheritdoc />
    public override Color EyeColor => IrisColor;

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
            DrawUpright(context, state);
        }
    }

    private static void DrawUpright(DrawingContext context, AnimalRenderState state)
    {
        var phase = state.Phase;
        var isDragged = state.State == AnimalAnimationState.Dragged;

        var gait = state.State switch
        {
            AnimalAnimationState.Run => new Gait(RunReach, 4, RunStanceFraction),
            AnimalAnimationState.Walk => new Gait(WalkReach, 2, WalkStanceFraction),
            _ => new Gait(0, 0, 1),
        };

        // A waddle: the body bobs twice per cycle, lowest when a pair of legs is in the middle of the stance.
        var midStance = 2 * (phase - (Math.PI * gait.StanceFraction));
        var bob = state.State switch
        {
            AnimalAnimationState.Run => -(1 - Math.Cos(midStance)) * 1.2,
            AnimalAnimationState.Walk => Math.Cos(midStance) * 1.2,
            AnimalAnimationState.Dragged => (Math.Sin(phase * 0.5) * 1.5) - 2,
            _ => Math.Sin(phase * 0.5) * 0.6,
        };

        if (!isDragged)
        {
            context.DrawEllipse(ShadowBrush, null, new Point(50, GroundY + 2), 27, 4);
        }

        // Tail stub.
        context.DrawEllipse(Fur, OutlinePen, new Point(27, 70 + bob), 4, 3.5);

        DrawLeg(context, 35, FarHipY, bob, phase, gait, isDragged, true);
        DrawLeg(context, 57, FarHipY, bob, phase + Math.PI, gait, isDragged, true);

        // Black body with a white saddle on the back.
        context.DrawEllipse(Patch, OutlinePen, new Point(45, 72 + bob), 18, 12.5);
        context.DrawEllipse(Fur, null, new Point(43, 70 + bob), 13, 8);

        DrawLeg(context, 40, NearHipY, bob, phase + Math.PI, gait, isDragged, false);
        DrawLeg(context, 60, NearHipY, bob, phase, gait, isDragged, false);

        DrawHead(context, bob, state);
    }

    private static void DrawLeg(
        DrawingContext context,
        double hipX,
        double hipY,
        double bob,
        double phase,
        Gait gait,
        bool isDragged,
        bool isFar)
    {
        var hip = new Point(hipX, hipY + bob);
        Point foot;
        if (isDragged)
        {
            foot = new Point(hipX + (Math.Sin(phase) * 2), hip.Y + DanglingLegLength);
        }
        else
        {
            var (offsetX, lift) = GetFootOffset(phase, gait);
            foot = new Point(hipX + offsetX, GroundY - lift);
        }

        DrawLimb(context, isFar ? PatchFar : Patch, hip, foot);
    }

    private static void DrawHead(DrawingContext context, double bob, AnimalRenderState state)
    {
        var isDragged = state.State == AnimalAnimationState.Dragged;

        using (context.PushTransform(Matrix.CreateTranslation(0, bob * 0.6)))
        {
            // Ears are drawn behind the head so their base is hidden.
            DrawEar(context, new Point(57, 31));
            DrawEar(context, new Point(84, 30));

            context.DrawEllipse(Fur, OutlinePen, new Point(70, 46), 20, 18.5);
            foreach (var (center, angle) in Patches)
            {
                DrawTiltedEllipse(context, Patch, center, 5.5, 7, angle);
            }

            for (var i = 0; i < EyeCenters.Length; i++)
            {
                if (state.IsBlinking)
                {
                    context.DrawGeometry(null, ClosedEyePen, ClosedEyes[i]);
                }
                else
                {
                    DrawEye(context, EyeCenters[i], isDragged);
                }
            }

            context.DrawEllipse(Blush, null, new Point(59, 55), 3.2, 2);
            context.DrawEllipse(Blush, null, new Point(82.5, 55), 3.2, 2);

            context.DrawGeometry(Pupil, null, Nose);
            if (isDragged)
            {
                // Surprised open mouth.
                context.DrawEllipse(Pupil, null, new Point(70.8, 57), 1.9, 1.4);
            }
            else
            {
                context.DrawGeometry(null, FacePen, MouthLeft);
                context.DrawGeometry(null, FacePen, MouthRight);
            }
        }
    }

    private static void DrawEar(DrawingContext context, Point center)
    {
        context.DrawEllipse(Patch, OutlinePen, center, 6.5, 6.5);
        context.DrawEllipse(EarInner, null, center, 3, 3);
    }

    private static void DrawEye(DrawingContext context, Point center, bool isDragged)
    {
        // Wide open and with small pupils when the cub is lifted.
        var radius = isDragged ? 3.6 : 3.2;
        context.DrawEllipse(Brushes.White, null, center, radius, radius);
        context.DrawEllipse(Iris, null, new Point(center.X + 0.5, center.Y + 0.3), 2.1, 2.1);
        context.DrawEllipse(Pupil, null, new Point(center.X + 0.6, center.Y + 0.4), isDragged ? 0.8 : 1.2, isDragged ? 0.8 : 1.2);
        context.DrawEllipse(Brushes.White, null, new Point(center.X - 0.7, center.Y - 1), 0.9, 0.9);
    }

    private static void DrawSleeping(DrawingContext context, double phase)
    {
        var breathe = Math.Sin(phase);

        context.DrawEllipse(ShadowBrush, null, new Point(50, GroundY + 2), 27, 4);

        // Sitting with the legs stretched forward.
        DrawLimb(context, Patch, new Point(41, 85), new Point(35, GroundY - 1.5));
        DrawLimb(context, Patch, new Point(59, 85), new Point(65, GroundY - 1.5));

        // Body, it swells with the breath while sitting on the ground.
        var breath = Matrix.CreateTranslation(-50, -GroundY)
            * Matrix.CreateScale(1 + (breathe * 0.02), 1 + (breathe * 0.03))
            * Matrix.CreateTranslation(50, GroundY);
        using (context.PushTransform(breath))
        {
            context.DrawEllipse(Patch, OutlinePen, new Point(50, 76), 17, 14);
            context.DrawEllipse(Fur, null, new Point(50, 79), 10, 9);
            DrawTiltedEllipse(context, Patch, new Point(43, 74), 5, 6.5, 25, OutlinePen);
            DrawTiltedEllipse(context, Patch, new Point(57, 74), 5, 6.5, -25, OutlinePen);
        }

        // The head nods slowly, tilted to the side.
        var tilt = 14 + (breathe * 3);
        var nod = Matrix.CreateTranslation(-50, -52)
            * Matrix.CreateRotation(tilt * Math.PI / 180)
            * Matrix.CreateTranslation(50, 52);
        using (context.PushTransform(nod))
        {
            DrawEar(context, new Point(36, 37));
            DrawEar(context, new Point(64, 37));
            context.DrawEllipse(Fur, OutlinePen, new Point(50, 52), 20, 18);
            foreach (var (center, angle) in SleepingPatches)
            {
                DrawTiltedEllipse(context, Patch, center, 5.5, 7, angle);
            }

            foreach (var eye in SleepingClosedEyes)
            {
                context.DrawGeometry(null, ClosedEyePen, eye);
            }

            context.DrawEllipse(Blush, null, new Point(37, 61), 3.2, 2);
            context.DrawEllipse(Blush, null, new Point(63, 61), 3.2, 2);
            context.DrawGeometry(Pupil, null, SleepingNose);
            context.DrawGeometry(null, FacePen, SleepingMouthLeft);
            context.DrawGeometry(null, FacePen, SleepingMouthRight);

            // A drool bubble grows and shrinks with the breath.
            var bubble = 1.6 + (breathe * 0.6);
            context.DrawEllipse(Drool, null, new Point(52.6, 64.6 + bubble), bubble, bubble * 1.2);
        }

        DrawZzz(context, ZzzPen, new Point(70, 36), phase);
    }

    private static void DrawLimb(DrawingContext context, IBrush brush, Point from, Point to)
    {
        // The limb is drawn pointing down from the joint, then rotated towards the end.
        var length = Point.Distance(from, to);
        var angle = Math.Atan2(to.X - from.X, to.Y - from.Y);
        using (context.PushTransform(Matrix.CreateRotation(-angle) * Matrix.CreateTranslation(from.X, from.Y)))
        {
            context.DrawRectangle(
                brush,
                OutlinePen,
                new Rect(-LegWidth / 2, -LegWidth / 2, LegWidth, length + (LegWidth / 2)),
                LegWidth / 2,
                LegWidth / 2);
        }
    }

    private static void DrawTiltedEllipse(
        DrawingContext context,
        IBrush brush,
        Point center,
        double radiusX,
        double radiusY,
        double angleDegrees,
        IPen? pen = null)
    {
        var transform = Matrix.CreateRotation(angleDegrees * Math.PI / 180) * Matrix.CreateTranslation(center.X, center.Y);
        using (context.PushTransform(transform))
        {
            context.DrawEllipse(brush, pen, default, radiusX, radiusY);
        }
    }
}
