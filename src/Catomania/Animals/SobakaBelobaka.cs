using Avalonia;
using Avalonia.Media;
using Catomania.Models;
using Catomania.Simulation;
using static Catomania.Animals.SobakaBelobakaArtwork;

namespace Catomania.Animals;

/// <summary>
/// Sobaka-belobaka (Russian "Собака-белобака", the white doggy), a fluffy white puppy with floppy ears. It sits
/// facing the viewer and glances where the cursor is, wags the curled tail, walks sideways, sits up and begs, waves
/// hello when it barks, bows to play, squints happily when jumping for joy and sleeps lying with the head on the
/// front paws. The artwork is in <see cref="SobakaBelobakaArtwork" />.
/// </summary>
public sealed class SobakaBelobaka : Animal
{
    // The artwork is scaled down, so the sitting dog still fits into the design space at the top of a hop.
    private const double ArtScale = DesignSize / 850;

    // The paws take short steps, see Cat for how the stride is chosen. In the art coordinates.
    private const double WalkStanceFraction = 0.5;
    private const double RunStanceFraction = 0.35;
    private const double WalkReach = 36;
    private const double RunReach = 64;
    private const double WalkLift = 24;
    private const double RunLift = 40;
    private const double WalkStride = 2 * WalkReach / WalkStanceFraction;
    private const double RunStride = 2 * RunReach / RunStanceFraction;

    // How far a leg hangs below its hip, the swing of the leg is turned into an angle with it.
    private const double LegLength = 150;

    // In degrees.
    private const double TailSway = 6;
    private const double HappyTailWag = 16;
    private const double WalkTailWag = 10;
    private const double PawWave = 18;
    private const double RunLean = 4;

    // How much the chest rises when breathing, as a fraction of the height.
    private const double SittingBreath = 0.012;
    private const double SleepingBreath = 0.025;

    // Like Muchi, the dog glances to the side when the cursor is more to the side than up or down.
    private const double LookSideThreshold = 0.3;

    private static readonly Matrix ArtToDesign = Matrix.CreateScale(ArtScale, ArtScale)
        * Matrix.CreateTranslation(DesignSize / 2, GroundY);

    private static readonly IBrush ShadowBrush = new SolidColorBrush(Color.FromUInt32(0x28000000));
    private static readonly IPen ZzzPen = new Pen(
        new SolidColorBrush(Color.FromUInt32(0xFFC9D6E3)), 1.8, null, PenLineCap.Round, PenLineJoin.Round);

    // The pivots in the art coordinates, the origin is on the ground under the middle of the dog.
    private static readonly Point SittingTailBase = new(110, -140);
    private static readonly Point WalkingTailBase = new(-165, -310);
    private static readonly Point PlayingTailBase = new(-150, -405);
    private static readonly Point WavingShoulder = new(-90, -220);
    private static readonly Point FarHip = new(-104, -180);
    private static readonly Point FarShoulder = new(134, -180);
    private static readonly Point NearShoulder = new(5, -160);
    private static readonly Point NearHip = new(-240, -170);

    // From the eyes of the sitting dog to the eyes of the walking dog, the left eye first.
    private static readonly Vector[] WalkingEyeShifts = [new(119.3, -7.7), new(118.6, -22.9)];

    private enum Face
    {
        Ahead,
        Following,
        Blink,
        Happy,
    }

    /// <inheritdoc />
    public override AnimalKind Kind => AnimalKind.SobakaBelobaka;

    /// <inheritdoc />
    public override Color BodyColor => FurColor;

    /// <inheritdoc />
    public override Color EyeColor => InkColor;

    /// <inheritdoc />
    public override string Sound => "Gav-gav!";

    /// <inheritdoc />
    public override AnimalSimulatorOptions CreateSimulatorOptions() => new()
    {
        WalkStrideLength = WalkStride * ArtScale / DesignSize,
        RunStrideLength = RunStride * ArtScale / DesignSize,
    };

    /// <inheritdoc />
    protected override void Draw(DrawingContext context, AnimalRenderState state)
    {
        switch (state.State)
        {
            case AnimalAnimationState.Sleep:
                DrawSleeping(context, state.Phase);
                break;
            case AnimalAnimationState.Walk:
                DrawWalking(context, state, new Gait(WalkReach, WalkLift, WalkStanceFraction), 0, 6);
                break;
            case AnimalAnimationState.Run:
                DrawWalking(context, state, new Gait(RunReach, RunLift, RunStanceFraction), RunLean, 10);
                break;
            case AnimalAnimationState.Dragged:
                DrawBegging(context, state, isDragged: true);
                break;
            default:
                switch (state.Trick)
                {
                    // Sits up and begs with the paws held up.
                    case AnimalTrick.Emote:
                        DrawBegging(context, state, isDragged: false);
                        break;

                    // Barks and waves hello.
                    case AnimalTrick.Speak:
                        DrawWaving(context, state);
                        break;

                    // Bows to play with the rear up, the wiggle rocks it.
                    case AnimalTrick.Wiggle:
                        DrawPlaying(context, state);
                        break;
                    default:
                        DrawSitting(context, state);
                        break;
                }

                break;
        }
    }

    private static Face GetFace(AnimalRenderState state)
    {
        var face = state.Trick switch
        {
            AnimalTrick.Happy => Face.Happy,
            AnimalTrick.Shake => Face.Blink,
            AnimalTrick.LookAround => Face.Ahead,
            _ => Face.Following,
        };

        return state.IsBlinking && face != Face.Happy ? Face.Blink : face;
    }

    private static void DrawSitting(DrawingContext context, AnimalRenderState state)
    {
        var face = GetFace(state);
        var tail = face == Face.Happy
            ? Math.Sin(state.Phase * 3) * HappyTailWag
            : Math.Sin(state.Phase * 0.7) * TailSway;

        DrawShadow(context, ShadowBrush, state, DesignSize / 2, 25, 5);
        using (context.PushTransform(Breathe(state.Phase * 0.5, SittingBreath) * ArtToDesign))
        {
            DrawRotated(context, SittingTailBase, tail, SittingTail);
            SobakaBelobakaArtwork.Draw(context, SittingBody);
            if (face == Face.Happy)
            {
                SobakaBelobakaArtwork.Draw(context, HappyFace);
                return;
            }

            SobakaBelobakaArtwork.Draw(context, Nose);
            SobakaBelobakaArtwork.Draw(context, face switch
            {
                Face.Blink => ClosedEyes,
                Face.Following => GetFollowingEyes(state),
                _ => Eyes,
            });
            SobakaBelobakaArtwork.Draw(context, Cheeks);
        }
    }

    // The dog glances to the left or to the right where the cursor is, only the highlights in the eyes move.
    private static SobakaBelobakaArtwork.Layer[] GetFollowingEyes(AnimalRenderState state)
    {
        var direction = state.CursorDirection;
        var length = direction.Length;
        if (length < double.Epsilon)
        {
            return Eyes;
        }

        // AnimalView mirrors the dog when it faces left, so compensate to keep the look on the cursor side.
        var side = direction.X * (state.FacingRight ? 1 : -1) / length;
        return side > LookSideThreshold
            ? RightEyes
            : side < -LookSideThreshold
                ? LeftEyes
                : Eyes;
    }

    // Sits up with the front paws held up, the hind legs dangle and swing when the dog is picked up.
    private static void DrawBegging(DrawingContext context, AnimalRenderState state, bool isDragged)
    {
        var tail = Math.Sin(state.Phase * 3) * HappyTailWag;
        if (!isDragged)
        {
            DrawShadow(context, ShadowBrush, state, DesignSize / 2, 25, 5);
        }

        using (context.PushTransform(Breathe(state.Phase * 0.5, SittingBreath) * ArtToDesign))
        {
            DrawRotated(context, SittingTailBase, tail, BeggingTail);
            var dangle = isDragged
                ? Matrix.CreateTranslation(Math.Sin(state.Phase) * 8, 20)
                : Matrix.Identity;
            using (context.PushTransform(dangle))
            {
                SobakaBelobakaArtwork.Draw(context, BeggingHindLegs);
            }

            SobakaBelobakaArtwork.Draw(context, BeggingBody);
            SobakaBelobakaArtwork.Draw(context, CuteFace);
        }
    }

    // The raised paw waves a few times during the trick, rising and fading out smoothly.
    private static void DrawWaving(DrawingContext context, AnimalRenderState state)
    {
        var progress = Math.Clamp(state.TrickProgress, 0, 1);
        var wave = Math.Sin(progress * TwoPi * 4) * Math.Sin(Math.PI * progress) * PawWave;
        var tail = Math.Sin(state.Phase * 3) * HappyTailWag;

        DrawShadow(context, ShadowBrush, state, DesignSize / 2, 25, 5);
        using (context.PushTransform(ArtToDesign))
        {
            DrawRotated(context, SittingTailBase, tail, BeggingTail);
            SobakaBelobakaArtwork.Draw(context, BeggingHindLegs);
            SobakaBelobakaArtwork.Draw(context, WavingBody);
            SobakaBelobakaArtwork.Draw(context, WavingFace);
            DrawRotated(context, WavingShoulder, wave, WavingPaw);
        }
    }

    private static void DrawPlaying(DrawingContext context, AnimalRenderState state)
    {
        var tail = Math.Sin(state.Phase * 3) * HappyTailWag;

        DrawShadow(context, ShadowBrush, state, DesignSize / 2, 32, 5);
        using (context.PushTransform(ArtToDesign))
        {
            DrawRotated(context, PlayingTailBase, tail, PlayingTail);
            SobakaBelobakaArtwork.Draw(context, PlayingBody);
        }
    }

    // The dog walks and runs in the same pose, running only takes longer and higher steps.
    private static void DrawWalking(
        DrawingContext context, AnimalRenderState state, Gait gait, double lean, double bobHeight)
    {
        var phase = state.Phase;

        // The legs move in diagonal pairs and the body dips as each pair reaches the middle of its stance.
        var midStance = 2 * (phase - (Math.PI * gait.StanceFraction));
        var bob = Math.Cos(midStance) * bobHeight;
        var wag = Math.Sin(phase) * WalkTailWag;

        DrawShadow(context, ShadowBrush, state, DesignSize / 2, 30, 5);
        var tilt = Matrix.CreateRotation(lean * Math.PI / 180);
        using (context.PushTransform(tilt * ArtToDesign))
        {
            DrawLeg(context, FarHindLeg, FarHip, bob, phase + Math.PI, gait);
            DrawLeg(context, FarForeLeg, FarShoulder, bob, phase, gait);
            using (context.PushTransform(Matrix.CreateTranslation(0, bob)))
            {
                DrawRotated(context, WalkingTailBase, wag, WalkingTail);
                SobakaBelobakaArtwork.Draw(context, WalkingBody);
            }

            DrawLeg(context, NearForeLeg, NearShoulder, bob, phase + Math.PI, gait);
            DrawLeg(context, NearHindLeg, NearHip, bob, phase, gait);
            using (context.PushTransform(Matrix.CreateTranslation(0, bob)))
            {
                SobakaBelobakaArtwork.Draw(context, WalkingHead);
                if (state.IsBlinking)
                {
                    DrawWalkingClosedEyes(context);
                }
                else
                {
                    SobakaBelobakaArtwork.Draw(context, WalkingEyes);
                }

                SobakaBelobakaArtwork.Draw(context, WalkingCheeks);
            }
        }
    }

    // The side view has no closed eyes in the artwork, the closed eyes of the sitting dog are moved over the open ones.
    private static void DrawWalkingClosedEyes(DrawingContext context)
    {
        for (var i = 0; i < ClosedEyes.Length; i++)
        {
            var eye = ClosedEyes[i];
            using (context.PushTransform(Matrix.CreateTranslation(WalkingEyeShifts[i])))
            {
                context.DrawGeometry(eye.Fill, eye.Pen, eye.Geometry);
            }
        }
    }

    // The leg turns around the hip as the foot moves along the ground and goes up when it is lifted.
    private static void DrawLeg(
        DrawingContext context, SobakaBelobakaArtwork.Layer[] leg, Point hip, double bob, double phase, Gait gait)
    {
        var (offsetX, lift) = GetFootOffset(phase, gait);
        var angle = Math.Atan2(offsetX, LegLength);
        var transform = AroundPoint(hip, Matrix.CreateRotation(-angle))
            * Matrix.CreateTranslation(0, Math.Min(bob, 0) - lift);
        using (context.PushTransform(transform))
        {
            SobakaBelobakaArtwork.Draw(context, leg);
        }
    }

    private static void DrawSleeping(DrawingContext context, double phase)
    {
        context.DrawEllipse(ShadowBrush, null, new Point(DesignSize / 2, ShadowY), 38, 5);
        using (context.PushTransform(Breathe(phase, SleepingBreath) * ArtToDesign))
        {
            SobakaBelobakaArtwork.Draw(context, Sleeping);
        }

        DrawZzz(context, ZzzPen, new Point(70, 48), phase);
    }

    // The chest rises and falls, the paws stay on the ground.
    private static Matrix Breathe(double phase, double depth) => Matrix.CreateScale(1, 1 + (Math.Sin(phase) * depth));

    private static void DrawRotated(
        DrawingContext context, Point pivot, double angle, SobakaBelobakaArtwork.Layer[] layers)
    {
        using (context.PushTransform(AroundPoint(pivot, Matrix.CreateRotation(angle * Math.PI / 180))))
        {
            SobakaBelobakaArtwork.Draw(context, layers);
        }
    }

    private static Matrix AroundPoint(Point point, Matrix transform) => Matrix.CreateTranslation(-point.X, -point.Y)
        * transform
        * Matrix.CreateTranslation(point.X, point.Y);
}
