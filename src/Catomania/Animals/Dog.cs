using Avalonia;
using Avalonia.Media;
using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Animals;

/// <summary>
/// A liver and white springer spaniel drawn in profile: long legs, long ears that fly back when it runs
/// with a bound, it sleeps stretched out with the head on the paws and paddles in its dreams.
/// </summary>
public sealed class Dog : Animal
{
    // The long legs take longer steps than the cat's, see Cat for how the stride is chosen.
    private const double WalkStanceFraction = 0.5;
    private const double RunStanceFraction = 0.3;
    private const double WalkReach = 10;
    private const double RunReach = 17;
    private const double WalkStride = 2 * WalkReach / WalkStanceFraction;
    private const double RunStride = 2 * RunReach / RunStanceFraction;

    private const double FarHipY = 66;
    private const double NearHipY = 68;
    private const double LegWidth = 6.5;
    private const double DanglingLegLength = 20;

    private static readonly Color CoatColor = Color.FromUInt32(0xFFF7F4EE);
    private static readonly Color IrisColor = Color.FromUInt32(0xFFB8741C);

    private static readonly IBrush Coat = new SolidColorBrush(CoatColor);
    private static readonly IBrush CoatShade = new SolidColorBrush(Color.FromUInt32(0xFFDDD6CA));
    private static readonly IBrush Liver = new SolidColorBrush(Color.FromUInt32(0xFF6B3A26));
    private static readonly IBrush LiverFar = new SolidColorBrush(Color.FromUInt32(0xFF552C1C));
    private static readonly IBrush Iris = new SolidColorBrush(IrisColor);
    private static readonly IBrush Pupil = new SolidColorBrush(Color.FromUInt32(0xFF1E120C));
    private static readonly IBrush NoseBrush = new SolidColorBrush(Color.FromUInt32(0xFF3A1F16));
    private static readonly IBrush Tongue = new SolidColorBrush(Color.FromUInt32(0xFFE57F8D));
    private static readonly IBrush ShadowBrush = new SolidColorBrush(Color.FromUInt32(0x28000000));
    private static readonly IBrush OutlineBrush = new SolidColorBrush(Color.FromUInt32(0xFF24150E));
    private static readonly IBrush LiverDark = new SolidColorBrush(Color.FromUInt32(0xFF45231A));
    private static readonly IPen OutlinePen = new Pen(OutlineBrush, 1.3, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen ThinOutlinePen = new Pen(OutlineBrush, 1.1, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen MouthPen = new Pen(OutlineBrush, 1, null, PenLineCap.Round);
    private static readonly IPen StrandPen = new Pen(LiverDark, 1, null, PenLineCap.Round);
    private static readonly IPen TailStripePen = new Pen(Liver, 2.4, null, PenLineCap.Round);
    private static readonly IPen BlazePen = new Pen(Coat, 2.2, null, PenLineCap.Round);
    private static readonly IPen ClosedEyePen = new Pen(Coat, 1.3, null, PenLineCap.Round);
    private static readonly IPen ZzzPen = new Pen(
        new SolidColorBrush(Color.FromUInt32(0xFFC9D6E3)), 1.8, null, PenLineCap.Round, PenLineJoin.Round);

    // Freckles on the white legs, as offsets across the leg and fractions of its length.
    private static readonly (double X, double Fraction)[] LegFreckles = [(0.6, 0.36), (-0.8, 0.54), (0.4, 0.72)];

    private static readonly Point[] MuzzleFreckles = [new(86, 46), new(88.5, 45), new(84, 44.5)];

    // The tail is drawn pointing left from its base, the stripe is the liver feathering along the top.
    private static readonly Geometry Tail = Parse(
        "M2 -2 C-6 -6 -14 -8 -20 -10 C-17 -6 -12 -2 -6 1 C-10 3 -14 3 -18 2 C-12 6 -4 5 2 3 Z");

    private static readonly Geometry TailStripe = Parse("M2 -2 C-4 -5 -9 -7 -14 -8");

    private static readonly Geometry Saddle = Parse(
        "M24 57 C28 50 40 49 52 50 C60 51 64 54 63 58 C58 60 48 60 40 61 C32 62 26 61 24 57 Z");

    private static readonly Geometry BellyFringe = Parse("M30 70 Q33 75 36 71 Q39 76 42 71 Q45 75 48 70");

    private static readonly Geometry Neck = Parse("M58 58 C61 48 66 40 72 36 L80 42 C76 50 70 58 64 64 Z");

    private static readonly Geometry ChestRuff = Parse("M60 62 C64 56 68 52 72 50 C72 56 69 62 64 66 Z");

    private static readonly Geometry Muzzle = Parse("M80 37 C84 36 90 38 93 41 C95 44 94 48 90 49 C86 50 81 48 79 45 Z");

    private static readonly Geometry Blaze = Parse("M77 30 C80 33 81 36 80.5 39");

    private static readonly Geometry Brow = Parse("M75.5 32 Q78.5 30.5 81.5 32");

    private static readonly Geometry ClosedEye = Parse("M76 35.5 C77 37.5 79.5 37.5 80.5 35.5");

    private static readonly Geometry Mouth = Parse("M83 48.5 C86 50 89 50 92 48");

    private static readonly Geometry Ear = Parse(
        "M72 31 C65 33 63 42 63 50 C63 57 65 61 68 60 C70 62 73 61 73 58 C76 57 77 52 76 46 C76 38 76 33 72 31 Z");

    private static readonly Geometry EarStrands = Parse("M66 52 C66 56 67 58 68 59 M70 51 C70 55 71 58 72 59");

    private static readonly Geometry SleepingTail = Parse("M26 85 C18 85 10 87 4 89 C10 91 18 91 26 89 Z");

    private static readonly Geometry SleepingSaddle = Parse("M24 79 C28 73 44 72 56 73 C64 74 68 77 66 80 C56 80 40 80 24 79 Z");

    private static readonly Geometry SleepingMuzzle = Parse("M74 76 C79 76 86 78 89 80 C91 82 89 85 85 85 C80 85 75 83 73 80 Z");

    private static readonly Geometry SleepingEye = Parse("M71 73 C72 75 74.5 75 75.5 73");

    private static readonly Geometry SleepingEar = Parse(
        "M66 70 C60 72 58 80 59 86 C59 90 62 91 64 89 C66 91 69 90 68 87 C70 82 70 74 66 70 Z");

    /// <inheritdoc />
    public override AnimalKind Kind => AnimalKind.Dog;

    /// <inheritdoc />
    public override Color BodyColor => CoatColor;

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
            AnimalAnimationState.Run => new Gait(RunReach, 7, RunStanceFraction),
            AnimalAnimationState.Walk => new Gait(WalkReach, 3.5, WalkStanceFraction),
            _ => new Gait(0, 0, 1),
        };

        // The body is lowest when a pair of legs is in the middle of the stance, the run is a deeper bound.
        var midStance = 2 * (phase - (Math.PI * gait.StanceFraction));
        var bob = state.State switch
        {
            AnimalAnimationState.Run => -(1 - Math.Cos(midStance)) * 2,
            AnimalAnimationState.Walk => Math.Cos(midStance),
            AnimalAnimationState.Dragged => (Math.Sin(phase * 0.5) * 1.5) - 2,
            _ => Math.Sin(phase * 0.5) * 0.6,
        };

        // While bounding the body pitches forward and back.
        var pitch = state.State == AnimalAnimationState.Run ? Math.Sin(phase * 2) * 3 : 0;

        if (!isDragged)
        {
            context.DrawEllipse(ShadowBrush, null, new Point(50, GroundY + 2), 32, 4);
        }

        var wag = state.State switch
        {
            AnimalAnimationState.Idle => Math.Sin(phase * 4) * 18,
            AnimalAnimationState.Walk => Math.Sin(phase * 2) * 10,
            AnimalAnimationState.Run => Math.Sin(phase * 2) * 5,
            _ => 40,
        };
        using (context.PushTransform(Matrix.CreateRotation(ToRadians(wag)) * Matrix.CreateTranslation(23, 57 + bob)))
        {
            context.DrawGeometry(Coat, OutlinePen, Tail);
            context.DrawGeometry(null, TailStripePen, TailStripe);
        }

        DrawLeg(context, 28, FarHipY, bob, phase, gait, isDragged, LiverFar, false);
        DrawLeg(context, 60, FarHipY, bob, phase + Math.PI, gait, isDragged, CoatShade, true);

        using (context.PushTransform(RotateAround(pitch, 45, 61) * Matrix.CreateTranslation(0, bob)))
        {
            context.DrawEllipse(Coat, OutlinePen, new Point(45, 61), 24, 10.5);
            context.DrawGeometry(Liver, null, Saddle);
            context.DrawGeometry(Coat, ThinOutlinePen, BellyFringe);
        }

        DrawLeg(context, 33, NearHipY, bob, phase + Math.PI, gait, isDragged, Liver, false);
        DrawLeg(context, 64, NearHipY, bob, phase, gait, isDragged, Coat, true);

        using (context.PushTransform(RotateAround(pitch * 1.5, 62, 60) * Matrix.CreateTranslation(0, bob * 0.8)))
        {
            DrawHead(context, state);
        }
    }

    private static void DrawLeg(
        DrawingContext context,
        double hipX,
        double hipY,
        double bob,
        double phase,
        Gait gait,
        bool isDragged,
        IBrush brush,
        bool hasFreckles)
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

        DrawLimb(context, brush, hip, foot, LegWidth, hasFreckles);
        context.DrawEllipse(
            brush, ThinOutlinePen, new Point(foot.X + 1.6, foot.Y - (LegWidth * 0.32)), LegWidth * 0.68, LegWidth * 0.42);
    }

    private static void DrawHead(DrawingContext context, AnimalRenderState state)
    {
        var phase = state.Phase;
        var isDragged = state.State == AnimalAnimationState.Dragged;
        var isMoving = state.State is AnimalAnimationState.Walk or AnimalAnimationState.Run;

        context.DrawGeometry(Liver, OutlinePen, Neck);
        context.DrawGeometry(Coat, null, ChestRuff);
        context.DrawEllipse(Liver, OutlinePen, new Point(75, 37), 10.5, 9.5);
        context.DrawGeometry(Coat, OutlinePen, Muzzle);
        context.DrawGeometry(null, BlazePen, Blaze);
        foreach (var freckle in MuzzleFreckles)
        {
            context.DrawEllipse(Liver, null, freckle, 0.6, 0.6);
        }

        context.DrawEllipse(NoseBrush, null, new Point(93.5, 41.5), 2.4, 2.2);
        context.DrawEllipse(Brushes.White, null, new Point(93, 40.6), 0.7, 0.45);

        if (state.IsBlinking && !isDragged)
        {
            context.DrawGeometry(null, ClosedEyePen, ClosedEye);
        }
        else
        {
            DrawEye(context, isDragged);
        }

        if (isDragged)
        {
            // Surprised open mouth.
            context.DrawEllipse(NoseBrush, null, new Point(90, 49.5), 1.6, 1.2);
        }
        else
        {
            context.DrawGeometry(null, MouthPen, Mouth);
            if (isMoving)
            {
                // Panting, the tongue flaps with the bound.
                var flap = state.State == AnimalAnimationState.Run ? Math.Sin(phase * 2) * 0.8 : 0;
                context.DrawEllipse(Tongue, MouthPen, new Point(86.5, 51.5 + flap), 1.7, 2.6);
            }
        }

        // The ear flies back at full run and hangs down when the dog is lifted.
        var flapAngle = state.State switch
        {
            AnimalAnimationState.Run => 32 + (Math.Sin(phase * 2) * 14),
            AnimalAnimationState.Walk => 4 + (Math.Sin(phase * 2) * 7),
            AnimalAnimationState.Dragged => 0,
            _ => Math.Sin(phase * 0.5) * 2,
        };
        using (context.PushTransform(RotateAround(flapAngle, 72, 32)))
        {
            context.DrawGeometry(Liver, OutlinePen, Ear);
            context.DrawGeometry(null, StrandPen, EarStrands);
        }
    }

    private static void DrawEye(DrawingContext context, bool isDragged)
    {
        if (isDragged)
        {
            // Wide open and with a small pupil when the dog is lifted.
            context.DrawEllipse(Brushes.White, null, new Point(78.5, 35.5), 2.8, 2.8);
            context.DrawEllipse(Iris, null, new Point(79, 35.7), 1.8, 1.8);
            context.DrawEllipse(Pupil, null, new Point(79.1, 35.8), 0.7, 0.7);
        }
        else
        {
            context.DrawEllipse(Iris, null, new Point(78.5, 35.5), 2.3, 2.4);
            context.DrawEllipse(Pupil, null, new Point(78.8, 35.7), 1.3, 1.3);
            context.DrawGeometry(null, StrandPen, Brow);
        }

        context.DrawEllipse(Brushes.White, null, new Point(77.9, 34.8), 0.7, 0.7);
    }

    private static void DrawSleeping(DrawingContext context, double phase)
    {
        var breathe = Math.Sin(phase);

        // Every few breaths a dream: the front paw paddles and the tail thumps.
        var dream = Math.Pow(Math.Max(0, Math.Sin(phase * 0.35)), 6);

        context.DrawEllipse(ShadowBrush, null, new Point(50, GroundY + 2), 36, 4);

        using (context.PushTransform(RotateAround(dream * Math.Sin(phase * 9) * 8, 26, 86)))
        {
            context.DrawGeometry(Coat, OutlinePen, SleepingTail);
        }

        // Lying stretched out, the body swells with the breath while staying on the ground.
        var breath = Matrix.CreateTranslation(-50, -GroundY)
            * Matrix.CreateScale(1 + (breathe * 0.02), 1 + (breathe * 0.035))
            * Matrix.CreateTranslation(50, GroundY);
        using (context.PushTransform(breath))
        {
            context.DrawEllipse(Coat, OutlinePen, new Point(46, 82), 25, 9.5);
            context.DrawGeometry(Liver, null, SleepingSaddle);
            context.DrawEllipse(Liver, OutlinePen, new Point(30, 82), 9, 8);
        }

        var paw = dream * Math.Sin(phase * 10) * 2.5;
        DrawLimb(context, Coat, new Point(60, 86), new Point(80 + paw, 89), 6, false);
        context.DrawEllipse(Coat, ThinOutlinePen, new Point(81.5 + paw, 89.5), 4, 2.5);

        // The head rests on the paws and rises a little with the breath.
        using (context.PushTransform(RotateAround(breathe, 75, 88)))
        {
            context.DrawEllipse(Liver, OutlinePen, new Point(70, 74), 9.5, 8.5);
            context.DrawGeometry(Coat, OutlinePen, SleepingMuzzle);
            context.DrawEllipse(NoseBrush, null, new Point(89.5, 80.5), 2.2, 2);
            context.DrawGeometry(null, ClosedEyePen, SleepingEye);
            context.DrawGeometry(Liver, OutlinePen, SleepingEar);
        }

        DrawZzz(context, ZzzPen, new Point(80, 60), phase);
    }

    private static void DrawLimb(DrawingContext context, IBrush brush, Point from, Point to, double width, bool hasFreckles)
    {
        // The limb is drawn pointing down from the joint, then rotated towards the end.
        var length = Point.Distance(from, to);
        var angle = Math.Atan2(to.X - from.X, to.Y - from.Y);
        using (context.PushTransform(Matrix.CreateRotation(-angle) * Matrix.CreateTranslation(from.X, from.Y)))
        {
            context.DrawRectangle(
                brush,
                OutlinePen,
                new Rect(-width / 2, -width / 2, width, length + (width / 2)),
                width / 2,
                width / 2);

            if (hasFreckles)
            {
                foreach (var (x, fraction) in LegFreckles)
                {
                    context.DrawEllipse(Liver, null, new Point(x, length * fraction), 0.8, 0.8);
                }
            }
        }
    }

    private static Matrix RotateAround(double angleDegrees, double x, double y)
        => Matrix.CreateTranslation(-x, -y) * Matrix.CreateRotation(ToRadians(angleDegrees)) * Matrix.CreateTranslation(x, y);

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static Geometry Parse(string pathData) => StreamGeometry.Parse(pathData);
}
