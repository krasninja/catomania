using Avalonia;
using Avalonia.Media;
using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Animals;

/// <summary>
/// A rosy piglet with a round front-facing face, folded ears and a curled tail. It trots on short legs,
/// bounds when running and sleeps lying down with its head resting on a front hoof.
/// </summary>
public sealed class Pig : Animal
{
    // The short legs take shorter steps than the cat's, see Cat for how the stride is chosen.
    private const double WalkStanceFraction = 0.5;
    private const double RunStanceFraction = 0.35;
    private const double WalkReach = 6;
    private const double RunReach = 10;
    private const double WalkStride = 2 * WalkReach / WalkStanceFraction;
    private const double RunStride = 2 * RunReach / RunStanceFraction;

    private const double HipY = 71;
    private const double LegWidth = 7.2;
    private const double DanglingLegLength = 19;

    private static readonly Color SkinColor = Color.FromUInt32(0xFFF2A2B1);
    private static readonly Color IrisColor = Color.FromUInt32(0xFF9C5C28);

    private static readonly IBrush Skin = new SolidColorBrush(SkinColor);
    private static readonly IBrush SkinDark = new SolidColorBrush(Color.FromUInt32(0xFFD77F92));
    private static readonly IBrush Belly = new SolidColorBrush(Color.FromUInt32(0xFFF7BDC7));
    private static readonly IBrush Snout = new SolidColorBrush(Color.FromUInt32(0xFFF8C2CB));
    private static readonly IBrush EarInner = new SolidColorBrush(Color.FromUInt32(0xFFD97C90));
    private static readonly IBrush Outline = AnimalStyle.Ink;
    private static readonly IBrush Iris = new SolidColorBrush(IrisColor);
    private static readonly IBrush Hoof = new SolidColorBrush(Color.FromUInt32(0xFF86515F));
    private static readonly IBrush Blush = new SolidColorBrush(Color.FromUInt32(0xFFE78CA1));
    private static readonly IBrush ShadowBrush = new SolidColorBrush(Color.FromUInt32(0x28000000));
    private static readonly IPen OutlinePen = AnimalStyle.InkPen;
    private static readonly IPen ThinOutlinePen = AnimalStyle.ThinInkPen;
    private static readonly IPen FacePen = AnimalStyle.FaceInkPen;
    private static readonly IPen ClosedEyePen = new Pen(Outline, 1.2, null, PenLineCap.Round);
    private static readonly IPen HoofPen = new Pen(Outline, 0.75, null, PenLineCap.Round);
    private static readonly IPen TailOutlinePen = new Pen(Outline, 2.2 + (AnimalStyle.InkWidth * 2), null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen TailPen = new Pen(Skin, 2.2, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen ZzzPen = new Pen(
        new SolidColorBrush(Color.FromUInt32(0xFFC9D6E3)), 1.8, null, PenLineCap.Round, PenLineJoin.Round);

    // The tail is drawn from its base at the origin, curling back towards the body.
    private static readonly Geometry Tail = Parse(
        "M0 0 C-7 -5 -12 2 -8 5 C-3 8 -2 1 -6 1 C-10 1 -9 5 -7 4");

    private static readonly Geometry LeftEar = Parse(
        "M61 37 C54 33 52 24 56 23 C63 24 67 31 67 35 Z");

    private static readonly Geometry RightEar = Parse(
        "M78 34 C83 25 91 24 92 29 C92 35 87 39 83 40 Z");

    private static readonly Geometry LeftEarInner = Parse("M59 34 Q56 27 57 26 Q62 29 63 33 Z");
    private static readonly Geometry RightEarInner = Parse("M83 33 Q87 27 89 28 Q89 33 86 35 Z");
    private static readonly Geometry Mouth = Parse("M71 60.2 Q73 61.7 75 60.2");

    private static readonly Point[] EyeCenters = [new(66, 43), new(78, 43)];

    private static readonly Geometry[] ClosedEyes = Array.ConvertAll(EyeCenters, center => CreateCurve(
        new Point(center.X - 2.8, center.Y), new Point(center.X - 1.4, center.Y + 2.6),
        new Point(center.X + 1.4, center.Y + 2.6), new Point(center.X + 2.8, center.Y)));

    private static readonly Geometry SleepingLeftEar = Parse(
        "M20 67 C12 60 10 68 15 73 Z");

    private static readonly Geometry SleepingRightEar = Parse(
        "M35 63 C41 56 47 62 43 70 Z");

    private static readonly Point[] SleepingEyeCenters = [new(24, 71), new(36, 71)];

    private static readonly Geometry[] SleepingClosedEyes = Array.ConvertAll(SleepingEyeCenters, center => CreateCurve(
        new Point(center.X - 2.8, center.Y), new Point(center.X - 1.4, center.Y + 2.6),
        new Point(center.X + 1.4, center.Y + 2.6), new Point(center.X + 2.8, center.Y)));

    private static readonly Geometry SleepingMouth = Parse("M29 88.2 Q31 89.7 33 88.2");

    /// <inheritdoc />
    public override AnimalKind Kind => AnimalKind.Pig;

    /// <inheritdoc />
    public override Color BodyColor => SkinColor;

    /// <inheritdoc />
    public override Color EyeColor => IrisColor;

    /// <inheritdoc />
    public override string Sound => "Oink!";

    /// <inheritdoc />
    public override AnimalSimulatorOptions CreateSimulatorOptions() => new()
    {
        WalkStrideLength = WalkStride / DesignSize,
        RunStrideLength = RunStride / DesignSize,
    };

    /// <inheritdoc />
    protected override void Draw(DrawingContext context, AnimalRenderState state)
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
        var isRunning = state.State == AnimalAnimationState.Run;

        var gait = state.State switch
        {
            AnimalAnimationState.Run => new Gait(RunReach, 4, RunStanceFraction),
            AnimalAnimationState.Walk => new Gait(WalkReach, 2.5, WalkStanceFraction),
            _ => new Gait(0, 0, 1),
        };

        // A trot: the body dips as each diagonal pair of legs reaches the middle of its stance.
        var midStance = 2 * (phase - (Math.PI * gait.StanceFraction));
        var bob = state.State switch
        {
            AnimalAnimationState.Run => -(1 - Math.Cos(midStance)) * 1.2,
            AnimalAnimationState.Walk => Math.Cos(midStance) * 0.8,
            AnimalAnimationState.Dragged => (Math.Sin(phase * 0.5) * 1.5) - 2,
            _ => Math.Sin(phase * 0.5) * 0.6,
        };
        var stretch = isRunning ? 1.03 : 1.0;

        if (!isDragged)
        {
            DrawShadow(context, ShadowBrush, state, 48, 31 * stretch, 4);
        }

        DrawTail(context, 25, 60 + bob, phase, state.State);
        DrawLeg(context, 30, bob, phase, gait, isDragged, true);
        DrawLeg(context, 60, bob, phase + Math.PI, gait, isDragged, true);

        context.DrawEllipse(Skin, OutlinePen, new Point(46, 65 + bob), 24 * stretch, 14);
        context.DrawEllipse(Belly, null, new Point(48, 71.3 + bob), 15.5, 6);

        DrawLeg(context, 38, bob, phase + Math.PI, gait, isDragged, false);
        DrawLeg(context, 68, bob, phase, gait, isDragged, false);

        DrawHead(context, bob, state);
    }

    private static void DrawTail(DrawingContext context, double x, double y, double phase, AnimalAnimationState state)
    {
        var wiggle = state switch
        {
            AnimalAnimationState.Idle => Math.Sin(phase * 2) * 8,
            AnimalAnimationState.Walk => Math.Sin(phase * 2) * 12,
            AnimalAnimationState.Run => Math.Sin(phase * 2) * 16,
            _ => 0,
        };
        using (context.PushTransform(Matrix.CreateRotation(ToRadians(wiggle)) * Matrix.CreateTranslation(x, y)))
        {
            context.DrawGeometry(null, TailOutlinePen, Tail);
            context.DrawGeometry(null, TailPen, Tail);
        }
    }

    private static void DrawLeg(
        DrawingContext context,
        double hipX,
        double bob,
        double phase,
        Gait gait,
        bool isDragged,
        bool isFar)
    {
        var hip = new Point(hipX, HipY + bob);
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

        var length = Point.Distance(hip, foot);
        var angle = Math.Atan2(foot.X - hip.X, foot.Y - hip.Y);
        using (context.PushTransform(Matrix.CreateRotation(-angle) * Matrix.CreateTranslation(hip.X, hip.Y)))
        {
            context.DrawRectangle(
                isFar ? SkinDark : Skin,
                OutlinePen,
                new Rect(-LegWidth / 2, 0, LegWidth, length),
                3.2,
                3.2);
            context.DrawEllipse(Hoof, null, new Point(0, length - 1.5), 3.1, 2);
            context.DrawLine(HoofPen, new Point(0, length - 3), new Point(0, length - 0.2));
        }
    }

    private static void DrawHead(DrawingContext context, double bob, AnimalRenderState state)
    {
        var isDragged = state.State == AnimalAnimationState.Dragged;
        using (context.PushTransform(Matrix.CreateTranslation(0, bob * 0.6)))
        {
            context.DrawGeometry(Skin, OutlinePen, LeftEar);
            context.DrawGeometry(Skin, OutlinePen, RightEar);
            context.DrawGeometry(EarInner, null, LeftEarInner);
            context.DrawGeometry(EarInner, null, RightEarInner);

            context.DrawEllipse(Skin, OutlinePen, new Point(72, 47), 17, 16);
            DrawEyes(context, EyeCenters, ClosedEyes, state.IsBlinking, isDragged);

            context.DrawEllipse(Blush, null, new Point(61.5, 53), 2.8, 1.7);
            context.DrawEllipse(Blush, null, new Point(84.5, 53), 2.8, 1.7);
            context.DrawEllipse(Snout, ThinOutlinePen, new Point(73, 54), 9.2, 6.1);
            context.DrawEllipse(Outline, null, new Point(71, 53.7), 1.05, 1.45);
            context.DrawEllipse(Outline, null, new Point(77, 53.7), 1.05, 1.45);

            if (isDragged)
            {
                context.DrawEllipse(Outline, null, new Point(73, 61), 1.8, 1.25);
            }
            else
            {
                context.DrawGeometry(null, FacePen, Mouth);
            }
        }
    }

    private static void DrawSleeping(DrawingContext context, double phase)
    {
        var breathe = Math.Sin(phase) * 0.9;

        context.DrawEllipse(ShadowBrush, null, new Point(50, GroundY + 2), 34, 4.5);
        using (context.PushTransform(Matrix.CreateScale(0.8, 0.8) * Matrix.CreateTranslation(26, 78)))
        {
            context.DrawGeometry(null, TailOutlinePen, Tail);
            context.DrawGeometry(null, TailPen, Tail);
        }

        // The body swells with the breath while staying on the ground.
        var breath = Matrix.CreateTranslation(0, -GroundY)
            * Matrix.CreateScale(1, (12 + breathe) / 12)
            * Matrix.CreateTranslation(0, GroundY);
        using (context.PushTransform(breath))
        {
            context.DrawEllipse(Skin, OutlinePen, new Point(54, 80), 30, 12);
            context.DrawEllipse(Belly, null, new Point(57, 86), 20, 5);
        }

        context.DrawGeometry(Skin, OutlinePen, SleepingLeftEar);
        context.DrawGeometry(Skin, OutlinePen, SleepingRightEar);
        context.DrawEllipse(Skin, OutlinePen, new Point(30, 75), 17, 16);
        foreach (var eye in SleepingClosedEyes)
        {
            context.DrawGeometry(null, ClosedEyePen, eye);
        }

        context.DrawEllipse(Blush, null, new Point(19.5, 81), 2.8, 1.7);
        context.DrawEllipse(Blush, null, new Point(42.5, 81), 2.8, 1.7);
        context.DrawEllipse(Snout, ThinOutlinePen, new Point(31, 82), 9.2, 6.1);
        context.DrawEllipse(Outline, null, new Point(29, 81.7), 1.05, 1.45);
        context.DrawEllipse(Outline, null, new Point(35, 81.7), 1.05, 1.45);
        context.DrawGeometry(null, FacePen, SleepingMouth);

        context.DrawEllipse(Skin, OutlinePen, new Point(43, 88), 5.5, 3.3);
        context.DrawLine(HoofPen, new Point(42, 86.5), new Point(42, 89.5));

        DrawZzz(context, ZzzPen, new Point(59, 57), phase);
    }

    private static void DrawEyes(
        DrawingContext context,
        Point[] centers,
        Geometry[] closedEyes,
        bool isBlinking,
        bool isDragged)
    {
        for (var i = 0; i < centers.Length; i++)
        {
            var center = centers[i];
            if (isBlinking && !isDragged)
            {
                context.DrawGeometry(null, ClosedEyePen, closedEyes[i]);
                continue;
            }

            DrawGlossyEye(context, Iris, center, isDragged ? 3.6 : 3.2, isDragged ? 4.1 : 3.6, isDragged);
        }
    }

    private static Geometry Parse(string pathData) => StreamGeometry.Parse(pathData);

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
