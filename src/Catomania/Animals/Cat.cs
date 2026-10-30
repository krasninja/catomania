using Avalonia;
using Avalonia.Media;
using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Animals;

/// <summary>
/// A cat: walks and runs on four legs, wags its tail and curls up to sleep.
/// </summary>
public sealed class Cat : Animal
{
    // A leg is in the stance (planted) for this fraction of the gait cycle and swings forward for the rest.
    private const double WalkStanceFraction = 0.5;
    private const double RunStanceFraction = 0.35;

    // Distance a foot moves along the body during the stance, in either direction from under the hip.
    private const double WalkReach = 9;
    private const double RunReach = 14;

    // A planted foot moves back by 2 * reach while the body moves forward by stride * stance fraction,
    // the strides are chosen so that both are equal and the feet do not slide.
    private const double WalkStride = 2 * WalkReach / WalkStanceFraction;
    private const double RunStride = 2 * RunReach / RunStanceFraction;

    private const double HipY = 70;
    private const double DanglingLegLength = 20;

    // The shapes that never change are created once.
    private static readonly Geometry LeftEar = CreateTriangle(new Point(62, 40), new Point(63, 21), new Point(74, 30));

    private static readonly Geometry RightEar = CreateTriangle(new Point(78, 30), new Point(88, 20), new Point(87, 37));

    private static readonly Geometry LeftEarInner = CreateTriangle(
        new Point(64.5, 37), new Point(65, 26), new Point(71.5, 32));

    private static readonly Geometry RightEarInner = CreateTriangle(
        new Point(79.5, 32), new Point(85.5, 25.5), new Point(85, 36));

    private static readonly Geometry Nose = CreateTriangle(
        new Point(74.2, 49.2), new Point(78.6, 49.2), new Point(76.4, 51.7));

    private static readonly Geometry MouthLeft = CreateCurve(
        new Point(76.4, 51.7), new Point(76, 54), new Point(74.5, 54.6), new Point(73.2, 53.8));

    private static readonly Geometry MouthRight = CreateCurve(
        new Point(76.4, 51.7), new Point(77, 54), new Point(78.4, 54.6), new Point(79.6, 53.8));

    private static readonly double[] EyeXs = [69, 79];

    private static readonly Geometry[] ClosedEyes = Array.ConvertAll(EyeXs, x => CreateCurve(
        new Point(x - 3, 42.5), new Point(x - 1.5, 46), new Point(x + 1.5, 46), new Point(x + 3, 42.5)));

    private static readonly Geometry SleepingTail = CreateCurve(
        new Point(76, 84), new Point(92, 90), new Point(66, 97), new Point(38, 92));

    private static readonly Geometry SleepingLeftEar = CreateTriangle(
        new Point(19, 67), new Point(18, 53), new Point(29, 60));

    private static readonly Geometry SleepingRightEar = CreateTriangle(
        new Point(31, 62), new Point(39, 54), new Point(38, 67));

    private static readonly Geometry SleepingLeftEarInner = CreateTriangle(
        new Point(21.5, 64.5), new Point(21, 57), new Point(27, 61));

    private static readonly Geometry SleepingRightEarInner = CreateTriangle(
        new Point(32.5, 63), new Point(36.5, 58.5), new Point(36, 65));

    private static readonly Geometry SleepingLeftEye = CreateCurve(
        new Point(21, 72.5), new Point(22.5, 76), new Point(25, 76), new Point(26.5, 72.5));

    private static readonly Geometry SleepingRightEye = CreateCurve(
        new Point(30, 72.5), new Point(31.5, 76), new Point(34, 76), new Point(35.5, 72.5));

    private static readonly Geometry SleepingNose = CreateTriangle(
        new Point(26.6, 78.4), new Point(30.4, 78.4), new Point(28.5, 80.4));

    /// <summary>
    /// Initializes a new instance of the <see cref="Cat" /> class.
    /// </summary>
    /// <param name="colorScheme">Fur color scheme.</param>
    public Cat(CatColorScheme colorScheme = CatColorScheme.Ginger)
    {
        ColorScheme = colorScheme;
        Palette = CatPalette.FromScheme(colorScheme);
    }

    /// <summary>
    /// Gets the fur color scheme.
    /// </summary>
    public CatColorScheme ColorScheme { get; }

    /// <summary>
    /// Gets the palette used to draw the cat.
    /// </summary>
    public CatPalette Palette { get; }

    /// <inheritdoc />
    public override AnimalKind Kind => AnimalKind.Cat;

    /// <inheritdoc />
    public override Color BodyColor => Palette.FurColor;

    /// <inheritdoc />
    public override Color EyeColor => Palette.EyeColor;

    /// <inheritdoc />
    public override string Sound => "Meow!";

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
            DrawSleeping(context, Palette, state.Phase);
        }
        else
        {
            DrawUpright(context, Palette, state);
        }
    }

    private static void DrawUpright(DrawingContext context, CatPalette palette, AnimalRenderState state)
    {
        var phase = state.Phase;
        var isDragged = state.State == AnimalAnimationState.Dragged;
        var isRunning = state.State == AnimalAnimationState.Run;

        var gait = state.State switch
        {
            AnimalAnimationState.Run => new Gait(RunReach, 5, RunStanceFraction),
            AnimalAnimationState.Walk => new Gait(WalkReach, 2.5, WalkStanceFraction),
            _ => new Gait(0, 0, 1),
        };

        // The legs move in diagonal pairs, so a pair is in the middle of the stance twice per cycle.
        // The body is lowest then.
        var midStance = 2 * (phase - (Math.PI * gait.StanceFraction));
        var bob = state.State switch
        {
            AnimalAnimationState.Run => -(1 - Math.Cos(midStance)) * 1.5,
            AnimalAnimationState.Walk => Math.Cos(midStance) * 1.2,
            AnimalAnimationState.Dragged => (Math.Sin(phase * 0.5) * 1.5) - 2,
            _ => Math.Sin(phase * 0.5) * 0.6,
        };
        var stretch = isRunning ? 1.05 : 1.0;

        if (!isDragged)
        {
            DrawShadow(context, palette.ShadowBrush, state, 50, 30 * stretch, 4);
        }

        DrawTail(context, palette, bob, phase, isDragged);
        DrawLeg(context, palette, 30, bob, phase, gait, isDragged, true);
        DrawLeg(context, palette, 60, bob, phase + Math.PI, gait, isDragged, true);

        // Body.
        context.DrawEllipse(palette.Fur, palette.OutlinePen, new Point(46, 64 + bob), 22 * stretch, 13);
        context.DrawEllipse(palette.Fur, null, new Point(30, 66 + bob), 10, 11);
        context.DrawEllipse(palette.Fur, null, new Point(62, 66 + bob), 10.5, 11.5);
        context.DrawEllipse(palette.Belly, null, new Point(48, 71 + bob), 15, 6);
        DrawStripes(context, palette, bob);

        DrawLeg(context, palette, 38, bob, phase + Math.PI, gait, isDragged, false);
        DrawLeg(context, palette, 68, bob, phase, gait, isDragged, false);

        DrawHead(context, palette, bob, state);
    }

    private static void DrawLeg(
        DrawingContext context,
        CatPalette palette,
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
            // Lifted from the ground, the legs hang and dangle.
            foot = new Point(hipX + (Math.Sin(phase) * 2.5), hip.Y + DanglingLegLength);
        }
        else
        {
            var (offsetX, lift) = GetFootOffset(phase, gait);
            foot = new Point(hipX + offsetX, GroundY - lift);
        }

        // The leg is drawn pointing down from the hip, then rotated towards the foot.
        var length = Point.Distance(hip, foot);
        var angle = Math.Atan2(foot.X - hip.X, foot.Y - hip.Y);
        using (context.PushTransform(Matrix.CreateRotation(-angle) * Matrix.CreateTranslation(hip.X, hip.Y)))
        {
            context.DrawRectangle(
                isFar ? palette.FurDark : palette.Fur,
                palette.OutlinePen,
                new Rect(-3.75, 0, 7.5, length),
                3.5,
                3.5);
            context.DrawEllipse(palette.Belly, null, new Point(0, length - 1.5), 3.4, 2);
        }
    }

    private static void DrawTail(DrawingContext context, CatPalette palette, double bob, double phase, bool isDragged)
    {
        // Each part of the tail follows the previous one with a delay, so the sway runs to the tip as a wave.
        double Sway(double delay) => Math.Sin((phase * 0.5) - delay) * 6;

        Geometry tail;
        Point tip;
        if (isDragged)
        {
            tip = new Point(26, 93);
            tail = CreateCurve(
                new Point(28, 66 + bob),
                new Point(20, 76 + bob + Sway(0)),
                new Point(20, 86),
                tip);
        }
        else
        {
            tip = new Point(13 + (Sway(1.2) * 0.5), 34 + bob + (Sway(1.2) * 1.1));
            tail = CreateCurve(
                new Point(26, 62 + bob),
                new Point(14, 60 + bob + (Sway(0) * 0.4)),
                new Point(8 + (Sway(0.6) * 0.3), 46 + bob + Sway(0.6)),
                tip);
        }

        context.DrawGeometry(null, palette.TailPen, tail);
        context.DrawEllipse(palette.FurDark, null, tip, 4.2, 4.2);
    }

    private static void DrawStripes(DrawingContext context, CatPalette palette, double bob)
    {
        for (var i = 0; i < 3; i++)
        {
            var x = 36 + (i * 8);
            context.DrawLine(
                palette.StripePen,
                new Point(x, 54 + bob),
                new Point(x + 1.5, 62 + bob));
        }
    }

    private static void DrawHead(DrawingContext context, CatPalette palette, double bob, AnimalRenderState state)
    {
        var isDragged = state.State == AnimalAnimationState.Dragged;

        using (context.PushTransform(Matrix.CreateTranslation(0, bob * 0.6)))
        {
            // Ears are drawn behind the head so their base is hidden.
            context.DrawGeometry(palette.Fur, palette.OutlinePen, LeftEar);
            context.DrawGeometry(palette.Fur, palette.OutlinePen, RightEar);
            context.DrawGeometry(palette.EarInner, null, LeftEarInner);
            context.DrawGeometry(palette.EarInner, null, RightEarInner);

            context.DrawEllipse(palette.Fur, palette.OutlinePen, new Point(74, 44), 13.5, 13.5);

            // Forehead stripes.
            context.DrawLine(palette.StripePen, new Point(70, 33.5), new Point(69.5, 37.5));
            context.DrawLine(palette.StripePen, new Point(75.5, 32.8), new Point(75.5, 37));

            // Muzzle.
            context.DrawEllipse(palette.Belly, null, new Point(76.5, 50), 8, 5.5);

            DrawEyes(context, palette, state);

            // Nose.
            context.DrawGeometry(palette.Nose, null, Nose);

            if (isDragged)
            {
                // Surprised open mouth.
                context.DrawEllipse(palette.Pupil, null, new Point(76.4, 54.6), 2.1, 1.5);
            }
            else
            {
                context.DrawGeometry(null, palette.FacePen, MouthLeft);
                context.DrawGeometry(null, palette.FacePen, MouthRight);
            }

            // Whiskers.
            context.DrawLine(palette.WhiskerPen, new Point(69, 49), new Point(56, 45.5));
            context.DrawLine(palette.WhiskerPen, new Point(68.5, 50.5), new Point(55, 50));
            context.DrawLine(palette.WhiskerPen, new Point(69, 52), new Point(56.5, 55.5));
            context.DrawLine(palette.WhiskerPen, new Point(84, 49), new Point(96, 46));
            context.DrawLine(palette.WhiskerPen, new Point(84.5, 50.5), new Point(97, 50.5));
            context.DrawLine(palette.WhiskerPen, new Point(84, 52), new Point(95.5, 55.5));
        }
    }

    private static void DrawEyes(DrawingContext context, CatPalette palette, AnimalRenderState state)
    {
        var isDragged = state.State == AnimalAnimationState.Dragged;
        for (var i = 0; i < EyeXs.Length; i++)
        {
            var x = EyeXs[i];
            var center = new Point(x, 43);
            if (state.IsBlinking)
            {
                context.DrawGeometry(null, palette.FacePen, ClosedEyes[i]);
                continue;
            }

            var radiusX = isDragged ? 3.6 : 3.2;
            var radiusY = isDragged ? 4.4 : 3.6;
            context.DrawEllipse(palette.Eye, null, center, radiusX, radiusY);
            if (isDragged)
            {
                context.DrawEllipse(palette.Pupil, null, center, 1.8, 1.8);
            }
            else
            {
                context.DrawEllipse(palette.Pupil, null, center, 1.2, 3);
            }

            context.DrawEllipse(Brushes.White, null, new Point(x - 1.1, 41.4), 0.9, 0.9);
        }
    }

    private static void DrawSleeping(DrawingContext context, CatPalette palette, double phase)
    {
        const double bodyRadiusY = 12;
        var breathe = Math.Sin(phase) * 0.9;

        context.DrawEllipse(palette.ShadowBrush, null, new Point(50, GroundY + 2), 34, 4.5);

        // Tail wrapped around the body.
        context.DrawGeometry(null, palette.TailPen, SleepingTail);
        context.DrawEllipse(palette.FurDark, null, new Point(38, 92), 4.2, 4.2);

        // Body, it rises and falls with the breath while lying on the ground.
        var breath = Matrix.CreateTranslation(0, -GroundY)
            * Matrix.CreateScale(1, (bodyRadiusY + breathe) / bodyRadiusY)
            * Matrix.CreateTranslation(0, GroundY);
        using (context.PushTransform(breath))
        {
            context.DrawEllipse(palette.Fur, palette.OutlinePen, new Point(52, GroundY - bodyRadiusY), 30, bodyRadiusY);
            context.DrawEllipse(palette.Belly, null, new Point(56, 86), 20, 5);
            for (var i = 0; i < 3; i++)
            {
                var x = 44 + (i * 9);
                context.DrawLine(palette.StripePen, new Point(x, 70), new Point(x + 1.5, 77));
            }
        }

        // Head resting on the paws.
        context.DrawGeometry(palette.Fur, palette.OutlinePen, SleepingLeftEar);
        context.DrawGeometry(palette.Fur, palette.OutlinePen, SleepingRightEar);
        context.DrawGeometry(palette.EarInner, null, SleepingLeftEarInner);
        context.DrawGeometry(palette.EarInner, null, SleepingRightEarInner);
        context.DrawEllipse(palette.Fur, palette.OutlinePen, new Point(28, 74), 12.5, 12.5);
        context.DrawEllipse(palette.Belly, null, new Point(29, 80), 7, 4.5);

        // Closed eyes.
        context.DrawGeometry(null, palette.FacePen, SleepingLeftEye);
        context.DrawGeometry(null, palette.FacePen, SleepingRightEye);

        context.DrawGeometry(palette.Nose, null, SleepingNose);

        context.DrawEllipse(palette.Belly, palette.OutlinePen, new Point(41, 88), 5.5, 3.5);

        DrawZzz(context, palette.ZzzPen, new Point(58, 56), phase);
    }
}
