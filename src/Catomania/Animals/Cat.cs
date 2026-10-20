using Avalonia;
using Avalonia.Media;
using Catomania.Models;

namespace Catomania.Animals;

/// <summary>
/// A cat: walks and runs on four legs, wags its tail and curls up to sleep.
/// </summary>
public sealed class Cat : Animal
{
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
    public override void Render(DrawingContext context, AnimalRenderState state)
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
        var isWalking = state.State == AnimalAnimationState.Walk;

        var bob = state.State switch
        {
            AnimalAnimationState.Run => -Math.Abs(Math.Sin(phase)) * 3,
            AnimalAnimationState.Walk => Math.Sin(phase) * 1.2,
            AnimalAnimationState.Dragged => (Math.Sin(phase * 0.6) * 1.5) - 2,
            _ => Math.Sin(phase * 0.5) * 0.6,
        };
        var legSwing = state.State switch
        {
            AnimalAnimationState.Run => 7,
            AnimalAnimationState.Walk => 4,
            AnimalAnimationState.Dragged => 2.5,
            _ => 0,
        };
        var legLift = isRunning ? 5 : isWalking ? 2.5 : 0;
        var stretch = isRunning ? 1.05 : 1.0;
        var tailSway = Math.Sin(phase * (isWalking || isRunning ? 1.4 : 0.6)) * 6;

        if (!isDragged)
        {
            context.DrawEllipse(palette.ShadowBrush, null, new Point(50, GroundY + 2), 30 * stretch, 4);
        }

        DrawTail(context, palette, bob, tailSway, isDragged);
        DrawLeg(context, palette, 30, bob, Math.Sin(phase), legSwing, legLift, true);
        DrawLeg(context, palette, 60, bob, Math.Sin(phase + Math.PI), legSwing, legLift, true);

        // Body.
        context.DrawEllipse(palette.Fur, palette.OutlinePen, new Point(46, 64 + bob), 22 * stretch, 13);
        context.DrawEllipse(palette.Fur, null, new Point(30, 66 + bob), 10, 11);
        context.DrawEllipse(palette.Fur, null, new Point(62, 66 + bob), 10.5, 11.5);
        context.DrawEllipse(palette.Belly, null, new Point(48, 71 + bob), 15, 6);
        DrawStripes(context, palette, bob);

        DrawLeg(context, palette, 38, bob, Math.Sin(phase + Math.PI), legSwing, legLift, false);
        DrawLeg(context, palette, 68, bob, Math.Sin(phase), legSwing, legLift, false);

        DrawHead(context, palette, bob, state);
    }

    private static void DrawLeg(
        DrawingContext context,
        CatPalette palette,
        double x,
        double bob,
        double cycle,
        double swing,
        double lift,
        bool isFar)
    {
        var brush = isFar ? palette.FurDark : palette.Fur;
        var offsetX = cycle * swing;
        var offsetY = Math.Max(0, cycle) * lift;
        var top = 70 + bob;
        var rect = new Rect(x + offsetX - 3.75, top - offsetY, 7.5, GroundY - top + offsetY);
        context.DrawRectangle(brush, palette.OutlinePen, rect, 3.5, 3.5);
        context.DrawEllipse(palette.Belly, null, new Point(rect.X + 3.75, GroundY - 1.5), 3.4, 2);
    }

    private static void DrawTail(DrawingContext context, CatPalette palette, double bob, double sway, bool isDragged)
    {
        Geometry tail;
        Point tip;
        if (isDragged)
        {
            tail = CreateCurve(
                new Point(28, 66 + bob),
                new Point(20, 76 + bob + sway),
                new Point(20, 86),
                new Point(26, 93));
            tip = new Point(26, 93);
        }
        else
        {
            tail = CreateCurve(
                new Point(26, 62 + bob),
                new Point(14, 60 + bob + (sway * 0.4)),
                new Point(8, 46 + bob + sway),
                new Point(13 + (sway * 0.3), 34 + bob + (sway * 1.1)));
            tip = new Point(13 + (sway * 0.3), 34 + bob + (sway * 1.1));
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
            context.DrawGeometry(palette.Fur, palette.OutlinePen, CreateTriangle(
                new Point(62, 40), new Point(63, 21), new Point(74, 30)));
            context.DrawGeometry(palette.Fur, palette.OutlinePen, CreateTriangle(
                new Point(78, 30), new Point(88, 20), new Point(87, 37)));
            context.DrawGeometry(palette.EarInner, null, CreateTriangle(
                new Point(64.5, 37), new Point(65, 26), new Point(71.5, 32)));
            context.DrawGeometry(palette.EarInner, null, CreateTriangle(
                new Point(79.5, 32), new Point(85.5, 25.5), new Point(85, 36)));

            context.DrawEllipse(palette.Fur, palette.OutlinePen, new Point(74, 44), 13.5, 13.5);

            // Forehead stripes.
            context.DrawLine(palette.StripePen, new Point(70, 33.5), new Point(69.5, 37.5));
            context.DrawLine(palette.StripePen, new Point(75.5, 32.8), new Point(75.5, 37));

            // Muzzle.
            context.DrawEllipse(palette.Belly, null, new Point(76.5, 50), 8, 5.5);

            DrawEyes(context, palette, state);

            // Nose.
            context.DrawGeometry(palette.Nose, null, CreateTriangle(
                new Point(74.2, 49.2), new Point(78.6, 49.2), new Point(76.4, 51.7)));

            if (isDragged)
            {
                // Surprised open mouth.
                context.DrawEllipse(palette.Pupil, null, new Point(76.4, 54.6), 2.1, 1.5);
            }
            else
            {
                context.DrawGeometry(null, palette.FacePen, CreateCurve(
                    new Point(76.4, 51.7), new Point(76, 54), new Point(74.5, 54.6), new Point(73.2, 53.8)));
                context.DrawGeometry(null, palette.FacePen, CreateCurve(
                    new Point(76.4, 51.7), new Point(77, 54), new Point(78.4, 54.6), new Point(79.6, 53.8)));
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
        foreach (var x in new[] { 69.0, 79.0 })
        {
            var center = new Point(x, 43);
            if (state.IsBlinking)
            {
                context.DrawGeometry(null, palette.FacePen, CreateCurve(
                    new Point(x - 3, 42.5), new Point(x - 1.5, 46), new Point(x + 1.5, 46), new Point(x + 3, 42.5)));
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
        var breathe = Math.Sin(phase) * 0.9;

        context.DrawEllipse(palette.ShadowBrush, null, new Point(50, GroundY + 2), 34, 4.5);

        // Tail wrapped around the body.
        context.DrawGeometry(null, palette.TailPen, CreateCurve(
            new Point(76, 84), new Point(92, 90), new Point(66, 97), new Point(38, 92)));
        context.DrawEllipse(palette.FurDark, null, new Point(38, 92), 4.2, 4.2);

        // Body.
        context.DrawEllipse(palette.Fur, palette.OutlinePen, new Point(52, 80), 30, 12 + breathe);
        context.DrawEllipse(palette.Belly, null, new Point(56, 86), 20, 5 + (breathe * 0.5));
        for (var i = 0; i < 3; i++)
        {
            var x = 44 + (i * 9);
            context.DrawLine(palette.StripePen, new Point(x, 70 + breathe), new Point(x + 1.5, 77 + breathe));
        }

        // Head resting on the paws.
        context.DrawGeometry(palette.Fur, palette.OutlinePen, CreateTriangle(
            new Point(19, 67), new Point(18, 53), new Point(29, 60)));
        context.DrawGeometry(palette.Fur, palette.OutlinePen, CreateTriangle(
            new Point(31, 62), new Point(39, 54), new Point(38, 67)));
        context.DrawGeometry(palette.EarInner, null, CreateTriangle(
            new Point(21.5, 64.5), new Point(21, 57), new Point(27, 61)));
        context.DrawGeometry(palette.EarInner, null, CreateTriangle(
            new Point(32.5, 63), new Point(36.5, 58.5), new Point(36, 65)));
        context.DrawEllipse(palette.Fur, palette.OutlinePen, new Point(28, 74), 12.5, 12.5);
        context.DrawEllipse(palette.Belly, null, new Point(29, 80), 7, 4.5);

        // Closed eyes.
        context.DrawGeometry(null, palette.FacePen, CreateCurve(
            new Point(21, 72.5), new Point(22.5, 76), new Point(25, 76), new Point(26.5, 72.5)));
        context.DrawGeometry(null, palette.FacePen, CreateCurve(
            new Point(30, 72.5), new Point(31.5, 76), new Point(34, 76), new Point(35.5, 72.5)));

        context.DrawGeometry(palette.Nose, null, CreateTriangle(
            new Point(26.6, 78.4), new Point(30.4, 78.4), new Point(28.5, 80.4)));

        context.DrawEllipse(palette.Belly, palette.OutlinePen, new Point(41, 88), 5.5, 3.5);

        DrawZzz(context, palette.ZzzPen, new Point(58, 56), phase);
    }
}
