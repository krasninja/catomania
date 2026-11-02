using Avalonia;
using Avalonia.Media;
using Catomania.Models;
using Catomania.Simulation;

namespace Catomania.Animals;

/// <summary>
/// A green tree frog sitting facing the viewer, with big round eyes on top of a wide head and splayed toes.
/// It gets around by hopping, hunts with a frown when running, snaps a fly with its tongue when speaking
/// and sleeps squatting with its head lowered.
/// </summary>
public sealed class Frog : Animal
{
    // The frog is drawn in the coordinates of its 800 x 800 artwork, the toes stand on the ground.
    private const double ArtScale = DesignSize / 800;
    private const double ArtGroundY = 634;

    // One hop per gait cycle. The feet stay planted for a short crouch and the frog flies the rest of the cycle.
    private const double HopStanceFraction = 0.3;
    private const double WalkStride = 40;
    private const double RunStride = 80;
    private const double WalkHopHeight = 6;
    private const double RunHopHeight = 11;
    private const double CrouchDepth = 14;

    // How far the body rises over the back legs in the stretched pose of a hop, in the art coordinates.
    private const double StretchHeight = 37;
    private const double SleepingHeadDrop = 108;

    private static readonly Color SkinColor = Color.FromUInt32(0xFFBDE28F);
    private static readonly Color PupilColor = Color.FromUInt32(0xFF141612);

    private static readonly Matrix ArtToDesign = Matrix.CreateScale(ArtScale, ArtScale)
        * Matrix.CreateTranslation(0, GroundY - (ArtGroundY * ArtScale));

    private static readonly IBrush Skin = new SolidColorBrush(SkinColor);
    private static readonly IBrush Belly = new SolidColorBrush(Color.FromUInt32(0xFFF5F2CE));
    private static readonly IBrush Cheek = new SolidColorBrush(Color.FromUInt32(0xFFF5CAC1));
    private static readonly IBrush Outline = new SolidColorBrush(PupilColor);
    private static readonly IBrush Tongue = new SolidColorBrush(Color.FromUInt32(0xFFEA9D8D));
    private static readonly IBrush FlyWing = new SolidColorBrush(Color.FromUInt32(0xFFBEBEBE));
    private static readonly IBrush FlyWingLight = new SolidColorBrush(Color.FromUInt32(0xFFD6D6D6));
    private static readonly IBrush ShadowBrush = new SolidColorBrush(Color.FromUInt32(0x28000000));

    // Drawn under the fill, so only the outer half of the stroke is visible.
    private static readonly IPen OutlinePen = new Pen(Outline, 24, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen FacePen = new Pen(Outline, 12, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen MouthRimPen = new Pen(Skin, 7);
    private static readonly IPen FlyOutlinePen = new Pen(Outline, 8, null, PenLineCap.Round, PenLineJoin.Round);
    private static readonly IPen FlyFacePen = new Pen(Outline, 4, null, PenLineCap.Round);
    private static readonly IPen ZzzPen = new Pen(
        new SolidColorBrush(Color.FromUInt32(0xFFC9D6E3)), 1.8, null, PenLineCap.Round, PenLineJoin.Round);

    private static readonly Geometry LeftBackLeg = Parse(
        "M274.775 499.175C259.602 476.752 226.386 446.81 203.195 460.416C180.003 474.022 203.23 528.834 "
        + "218.294 555.347C198.678 566.532 168.523 581.748 176.235 594.44C183.896 607.049 196.868 603.622 "
        + "213.84 596.434C207.109 613.204 213.84 625 227.241 625C252.784 625 257.582 562.851 257.582 562.851C"
        + "257.582 562.851 287.705 518.285 274.775 499.175Z");

    private static readonly Geometry RightBackLeg = Parse(
        "M525.225 499.175C540.398 476.752 573.614 446.81 596.805 460.416C619.997 474.022 596.77 528.834 "
        + "581.706 555.347C601.322 566.532 631.477 581.748 623.765 594.44C616.104 607.049 603.132 603.622 "
        + "586.16 596.434C592.891 613.204 586.16 625 572.759 625C547.216 625 542.418 562.851 542.418 562.851C"
        + "542.418 562.851 512.295 518.285 525.225 499.175Z");

    private static readonly Geometry Body = Parse(
        "M565 497C565 562.17 491.351 595 400.5 595C309.649 595 236 562.17 236 497C236 431.83 309.649 359 "
        + "400.5 359C491.351 359 565 431.83 565 497Z");

    private static readonly Geometry BellyPatch = Parse(
        "M506 554.973C506 588.387 458.766 597 400.5 597C342.234 597 295 588.387 295 554.973C295 521.56 "
        + "342.234 476 400.5 476C458.766 476 506 521.56 506 554.973Z");

    private static readonly Geometry LeftFrontLeg = Parse(
        "M293.461 626.062C287.156 616.055 303.961 596.062 303.961 596.062C303.961 596.062 268.456 523.397 "
        + "298.961 505.062C333.435 484.341 355.75 590.985 355.75 590.985C355.75 590.985 389.88 604.397 "
        + "378.168 618.625C367.233 631.908 353.848 617.129 350.45 614.104C351.7 624.164 350.519 633.28 "
        + "337.798 634.101C325.077 634.921 321.461 618.625 321.461 618.625C321.461 618.625 300.47 637.187 "
        + "293.461 626.062Z");

    private static readonly Geometry RightFrontLeg = Parse(
        "M507.092 626.062C513.397 616.055 496.592 596.062 496.592 596.062C496.592 596.062 532.097 523.397 "
        + "501.592 505.062C467.118 484.341 444.802 590.985 444.802 590.985C444.802 590.985 410.672 604.397 "
        + "422.385 618.625C433.32 631.908 446.705 617.129 450.103 614.104C448.853 624.164 450.034 633.28 "
        + "462.755 634.101C475.476 634.921 479.092 618.625 479.092 618.625C479.092 618.625 500.083 637.187 "
        + "507.092 626.062Z");

    // The front legs folded over the belly, for the cute pose.
    private static readonly Geometry LeftArm = Parse(
        "M310.566 541.972C310.566 541.972 306.009 567.689 316.473 573.202C328.105 579.332 337.003 552.762 "
        + "337.003 552.762C337.003 552.762 348.283 565.067 358.889 557.996C369.496 550.925 365.961 542.439 "
        + "359.848 534.352C364.304 535.272 383.285 541.379 386.113 524.408C389.143 506.231 352.879 511.68 "
        + "352.879 511.68C352.879 511.68 280.231 430.482 260.736 465.663C243.486 496.795 310.566 541.972 "
        + "310.566 541.972Z");

    private static readonly Geometry RightArm = Parse(
        "M490.076 542.192C490.076 542.192 494.633 567.909 484.17 573.423C472.537 579.552 463.639 552.982 "
        + "463.639 552.982C463.639 552.982 452.36 565.287 441.753 558.216C431.146 551.145 434.682 542.659 "
        + "440.795 534.572C436.339 535.493 417.358 541.599 414.529 524.628C411.5 506.451 447.763 511.9 "
        + "447.763 511.9C447.763 511.9 520.412 430.702 539.906 465.883C557.157 497.015 490.076 542.192 "
        + "490.076 542.192Z");

    private static readonly Geometry Head = Parse(
        "M536.5 132C572.675 132 602 161.325 602 197.5C602 211.088 597.861 223.709 590.777 234.173C610.605 "
        + "257.095 622 283.262 622 310C622 388.977 522.607 443 400 443C277.393 443 178 388.977 178 310C178 "
        + "283.009 189.611 256.599 209.787 233.522C202.969 223.188 199 210.807 199 197.5C199 161.325 228.325 "
        + "132 264.5 132C289.741 132 311.647 146.278 322.583 167.198C346.688 160.653 372.771 157 400 157C"
        + "427.559 157 453.944 160.741 478.292 167.437C489.186 146.387 511.163 132 536.5 132Z");

    // The front legs splayed out to the sides under the lowered head.
    private static readonly Geometry SleepingLeftFrontLeg = Parse(
        "M301.859 600.108C301.859 600.108 290.801 623.769 299.482 631.803C309.131 640.735 324.603 617.373 "
        + "324.603 617.373C324.603 617.373 332.313 632.178 344.389 628.093C356.464 624.008 355.245 614.897 "
        + "351.434 605.503C355.499 607.545 372.253 618.356 379.378 602.696C387.009 585.922 350.57 581.8 "
        + "350.57 581.8C350.57 581.8 301.413 484.565 273.477 513.503C248.757 539.109 301.859 600.108 "
        + "301.859 600.108Z");

    private static readonly Geometry SleepingRightFrontLeg = Parse(
        "M498.693 600.108C498.693 600.108 509.751 623.769 501.071 631.803C491.421 640.735 475.95 617.373 "
        + "475.95 617.373C475.95 617.373 468.239 632.178 456.164 628.093C444.089 624.008 445.307 614.897 "
        + "449.119 605.503C445.053 607.545 428.299 618.356 421.175 602.696C413.544 585.922 449.982 581.8 "
        + "449.982 581.8C449.982 581.8 499.139 484.565 527.075 513.503C551.795 539.109 498.693 600.108 "
        + "498.693 600.108Z");

    private static readonly Point[] EyeCenters = [new(262, 194), new(539, 194)];

    // Narrowed towards the nose, for the hunting and eating faces.
    private static readonly Geometry[] FocusedEyes =
    [
        Parse("M216 194C216 219.405 236.595 240 262 240C287.405 240 308 194 308 194C308 194 287.405 148 262 "
            + "148C236.595 148 216 168.595 216 194Z"),
        Parse("M493 194C493 194 513.595 240 539 240C564.405 240 585 219.405 585 194C585 168.595 564.405 148 "
            + "539 148C513.595 148 493 194 493 194Z"),
    ];

    private static readonly Geometry[] ClosedEyes =
    [
        Parse("M300.014 183C303.86 189.661 303.807 198.209 299.867 206.763C295.928 215.318 288.424 223.178 "
            + "279.007 228.615C269.59 234.052 259.031 236.62 249.653 235.755C240.275 234.889 232.846 230.661 "
            + "229 224"),
        Parse("M570.514 224C566.668 230.661 559.239 234.889 549.861 235.754C540.483 236.62 529.924 234.052 "
            + "520.507 228.615C511.09 223.178 503.586 215.318 499.647 206.763C495.707 198.209 495.654 189.661 "
            + "499.5 183"),
    ];

    private static readonly Geometry[] HappyEyes =
    [
        Parse("M228.746 201.559C225.631 196.163 225.674 189.239 228.865 182.309C232.057 175.379 238.135 "
            + "169.011 245.764 164.607C253.393 160.202 261.947 158.122 269.544 158.823C277.141 159.524 "
            + "283.159 162.949 286.274 168.345"),
        Parse("M571.408 201.559C574.524 196.163 574.481 189.239 571.29 182.309C568.098 175.379 562.019 "
            + "169.011 554.391 164.607C546.762 160.202 538.208 158.122 530.611 158.823C523.014 159.524 "
            + "516.996 162.949 513.88 168.345"),
    ];

    private static readonly Geometry Smile = Parse(
        "M441 260.75C441 268.441 436.68 275.818 428.991 281.256C421.302 286.695 410.874 289.75 400 289.75C"
        + "389.126 289.75 378.698 286.695 371.009 281.256C363.32 275.818 359 268.441 359 260.75");

    private static readonly Geometry HappySmile = Parse(
        "M442 194C442 201.691 437.68 209.068 429.991 214.506C422.302 219.945 411.874 223 401 223C390.126 223 "
        + "379.698 219.945 372.009 214.506C364.32 209.068 360 201.691 360 194");

    private static readonly Geometry CuteSmile = Parse(
        "M418 240C418 243.448 416.104 246.754 412.728 249.192C409.352 251.63 404.774 253 400 253C395.226 253 "
        + "390.648 251.63 387.272 249.192C383.896 246.754 382 243.448 382 240");

    private static readonly Geometry HuntingMouth = Parse(
        "M355.26 235.013C358.155 229.999 363.573 226.715 370.323 225.885C377.073 225.054 384.601 226.745 "
        + "391.252 230.585C397.903 234.425 403.131 240.099 405.787 246.36C408.442 252.62 408.308 258.954 "
        + "405.413 263.969");

    private static readonly Geometry SleepingMouth = Parse(
        "M352.883 278.327C351.539 280.655 348.943 282.133 345.666 282.435C342.388 282.737 338.699 281.84 "
        + "335.408 279.94C332.117 278.04 329.495 275.293 328.118 272.304C326.741 269.315 326.723 266.328 "
        + "328.067 264");

    private static readonly Point EatingMouthCenter = new(455, 261);
    private static readonly Geometry EatingMouthCrease = Parse("M493 261C493 281.987 475.987 299 455 299");

    // The tongue reaches up from the mouth to the fly.
    private static readonly Geometry TongueShape = Parse(
        "M693.003 148.491C693.003 148.491 729.628 103.174 753.565 114.955C785.608 130.725 717.454 210.687 "
        + "697.995 232.498C695.294 235.525 690.714 235.524 688.013 232.498C682.139 225.914 671.83 214.032 "
        + "661.177 200.217L461.07 272.29C454.835 274.535 447.959 271.301 445.713 265.066C443.468 258.831 "
        + "446.702 251.956 452.937 249.71L646.489 179.998C627.863 152.498 614.576 123.748 632.443 114.955C"
        + "656.345 103.191 692.896 148.359 693.003 148.491Z");

    private static readonly Geometry FlyBackWing = Parse(
        "M681.589 151.933C695.077 159.72 695.756 183.793 695.756 183.793C695.756 183.793 674.568 195.241 "
        + "661.081 187.454C647.594 179.667 641.251 165.403 646.914 155.594C652.577 145.785 668.102 144.146 "
        + "681.589 151.933Z");

    private static readonly Geometry FlyFrontWing = Parse(
        "M722.768 161.52C718.737 176.563 695.66 183.45 695.66 183.45C695.66 183.45 679.119 165.948 683.149 "
        + "150.905C687.18 135.861 699.317 126.043 710.257 128.975C721.198 131.906 726.799 146.477 722.768 "
        + "161.52Z");

    private static readonly Geometry FlyEyeCrosses = Parse(
        "M679.53 198.735L687.735 201.47M699.53 180.735L707.735 183.47M684.688 195.449L682.45 203.804"
        + "M704.688 177.449L702.45 185.804");

    private enum Face
    {
        Idle,
        LookAhead,
        Blink,
        Happy,
        Cute,
        Hunt,
        Eat,
        Startled,
    }

    /// <inheritdoc />
    public override AnimalKind Kind => AnimalKind.Frog;

    /// <inheritdoc />
    public override Color BodyColor => SkinColor;

    /// <inheritdoc />
    public override Color EyeColor => PupilColor;

    /// <inheritdoc />
    public override string Sound => "Ribbit!";

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
            return;
        }

        var pose = GetPose(state);
        if (state.State != AnimalAnimationState.Dragged)
        {
            // The shadow shrinks while the frog is in the air.
            var shrink = 1 - (0.35 * pose.Lift / RunHopHeight);
            DrawShadow(context, ShadowBrush, state, (DesignSize / 2) + pose.OffsetX, 31 * shrink, 5 * shrink);
        }

        using (context.PushTransform(ArtToDesign * Matrix.CreateTranslation(pose.OffsetX, -pose.Lift)))
        {
            DrawSitting(context, pose, GetFace(state), state.TrickProgress);
        }
    }

    private static Pose GetPose(AnimalRenderState state)
    {
        switch (state.State)
        {
            case AnimalAnimationState.Walk:
                return GetHopPose(state.Phase, WalkStride, WalkHopHeight);
            case AnimalAnimationState.Run:
                return GetHopPose(state.Phase, RunStride, RunHopHeight);
            case AnimalAnimationState.Dragged:
                return new Pose(0, 0, 1, Math.Sin(state.Phase * 0.5) * 6, 0, false);
        }

        var breathe = Math.Sin(state.Phase * 0.5) * 4;
        return new Pose(0, 0, GetTrickStretch(state), breathe, 0, state.Trick == AnimalTrick.Emote);
    }

    // The feet stay on the ground during the crouch: the frog is drawn shifted back as the window moves
    // forward, and catches up in the air.
    private static Pose GetHopPose(double phase, double stride, double height)
    {
        var progress = (phase % TwoPi) / TwoPi;
        var shift = stride * HopStanceFraction;
        if (progress < HopStanceFraction)
        {
            var stance = progress / HopStanceFraction;
            return new Pose(shift * (0.5 - stance), 0, 0, 0, Math.Sin(Math.PI * stance) * CrouchDepth, false);
        }

        var flight = (progress - HopStanceFraction) / (1 - HopStanceFraction);
        var arc = Math.Sin(Math.PI * flight);
        return new Pose(shift * (flight - 0.5), arc * height, arc, 0, 0, false);
    }

    // Stretched in the air during the hopping tricks, the hop itself is applied by the base class.
    private static double GetTrickStretch(AnimalRenderState state)
    {
        if (state.Trick is not (AnimalTrick.Hop or AnimalTrick.Spin or AnimalTrick.BunnyHops))
        {
            return 0;
        }

        var progress = Math.Clamp(state.TrickProgress, 0, 1);
        if (state.Trick == AnimalTrick.BunnyHops)
        {
            var hops = progress * AnimalSimulator.BunnyHopCount;
            progress = Math.Min(1, hops - Math.Min(Math.Floor(hops), AnimalSimulator.BunnyHopCount - 1));
        }

        const double crouch = AnimalSimulator.HopCrouchFraction;
        var flight = Math.Clamp((progress - crouch) / (1 - (2 * crouch)), 0, 1);
        return Math.Sin(Math.PI * flight);
    }

    private static Face GetFace(AnimalRenderState state)
    {
        var face = state.State switch
        {
            AnimalAnimationState.Dragged => Face.Startled,
            AnimalAnimationState.Run => Face.Hunt,
            AnimalAnimationState.Walk => Face.LookAhead,
            _ => state.Trick switch
            {
                AnimalTrick.Speak => state.TrickProgress < 0.5 ? Face.Eat : Face.Happy,
                AnimalTrick.Emote => Face.Cute,
                AnimalTrick.Wiggle => Face.Happy,
                AnimalTrick.Shake => Face.Blink,
                AnimalTrick.LookAround => Face.LookAhead,
                _ => Face.Idle,
            },
        };

        return state.IsBlinking && face is Face.Idle or Face.LookAhead or Face.Hunt or Face.Cute
            ? Face.Blink
            : face;
    }

    private static void DrawSitting(DrawingContext context, Pose pose, Face face, double trickProgress)
    {
        var rise = (StretchHeight * pose.Stretch) - pose.Crouch;

        DrawOutlined(context, Skin, LeftBackLeg);
        DrawOutlined(context, Skin, RightBackLeg);

        using (context.PushTransform(Matrix.CreateTranslation(0, (pose.Breathe * 0.5) - rise)))
        {
            DrawOutlined(context, Skin, Body);
            context.DrawGeometry(Belly, null, BellyPatch);
            if (pose.ArmsFolded)
            {
                DrawOutlined(context, Skin, LeftArm);
                DrawOutlined(context, Skin, RightArm);
            }
        }

        if (!pose.ArmsFolded)
        {
            // The front legs are tucked up under the body in the air, they dangle when the frog is dragged.
            var tuck = pose.Stretch * 66;
            using (context.PushTransform(Matrix.CreateTranslation(-5 * pose.Stretch, -rise - tuck)))
            {
                DrawOutlined(context, Skin, LeftFrontLeg);
            }

            using (context.PushTransform(Matrix.CreateTranslation(5 * pose.Stretch, -rise - tuck)))
            {
                DrawOutlined(context, Skin, RightFrontLeg);
            }
        }

        using (context.PushTransform(Matrix.CreateTranslation(0, pose.Breathe - rise)))
        {
            DrawHead(context, face, trickProgress);
        }
    }

    private static void DrawHead(DrawingContext context, Face face, double trickProgress)
    {
        DrawOutlined(context, Skin, Head);
        context.DrawEllipse(Cheek, null, new Point(247, 287), 39, 30);
        context.DrawEllipse(Cheek, null, new Point(553, 287), 39, 30);

        switch (face)
        {
            case Face.Blink:
                DrawClosedEyes(context, ClosedEyes);
                context.DrawGeometry(null, FacePen, Smile);
                break;
            case Face.Happy:
                DrawClosedEyes(context, HappyEyes);
                context.DrawGeometry(null, FacePen, HappySmile);
                break;
            case Face.Cute:
                DrawEyes(context, -1, true);
                context.DrawGeometry(null, FacePen, CuteSmile);
                break;
            case Face.Hunt:
                DrawFocusedEyes(context);
                context.DrawGeometry(null, FacePen, HuntingMouth);
                break;
            case Face.Eat:
                DrawFocusedEyes(context);
                DrawEating(context, trickProgress);
                break;
            case Face.Startled:
                DrawEyes(context, -1, true);
                context.DrawEllipse(Outline, null, new Point(400, 272), 17, 21);
                break;
            case Face.LookAhead:
                DrawEyes(context, 1, false);
                context.DrawGeometry(null, FacePen, Smile);
                break;
            default:
                DrawEyes(context, -1, false);
                context.DrawGeometry(null, FacePen, Smile);
                break;
        }
    }

    // The highlight is on the side the frog looks to: 1 is ahead, -1 is towards the nose.
    private static void DrawEyes(DrawingContext context, int look, bool isSparkling)
    {
        for (var i = 0; i < EyeCenters.Length; i++)
        {
            var center = EyeCenters[i];
            var side = look > 0 ? 1 : i == 0 ? 1 : -1;
            context.DrawEllipse(Outline, null, center, 46, 46);
            context.DrawEllipse(
                Brushes.White, null, new Point(center.X + (14.3 * side), center.Y - 17.7), 9.4, 9.4);
            if (isSparkling)
            {
                context.DrawEllipse(Brushes.White, null, new Point(center.X + (i == 0 ? -17 : 17), center.Y - 2), 18, 18);
            }
        }
    }

    private static void DrawFocusedEyes(DrawingContext context)
    {
        for (var i = 0; i < EyeCenters.Length; i++)
        {
            var center = EyeCenters[i];
            context.DrawGeometry(Outline, null, FocusedEyes[i]);
            context.DrawEllipse(Brushes.White, null, new Point(center.X + 14.3, center.Y - 17.7), 9.4, 9.4);
        }
    }

    private static void DrawClosedEyes(DrawingContext context, Geometry[] eyes)
    {
        foreach (var eye in eyes)
        {
            context.DrawGeometry(null, FacePen, eye);
        }
    }

    // The tongue shoots out to the fly and pulls it back into the mouth.
    private static void DrawEating(DrawingContext context, double trickProgress)
    {
        var progress = Math.Clamp(trickProgress / 0.5, 0, 1);
        var reach = progress < 0.25 ? progress / 0.25
            : progress < 0.6 ? 1
            : 1 - ((progress - 0.6) / 0.4);

        context.DrawEllipse(Outline, MouthRimPen, EatingMouthCenter, 24.5, 24.5);
        context.DrawGeometry(null, FacePen, EatingMouthCrease);
        if (reach <= 0)
        {
            return;
        }

        var extend = Matrix.CreateTranslation(-EatingMouthCenter.X, -EatingMouthCenter.Y)
            * Matrix.CreateScale(reach, reach)
            * Matrix.CreateTranslation(EatingMouthCenter.X, EatingMouthCenter.Y);
        using (context.PushTransform(extend))
        {
            context.DrawGeometry(Tongue, null, TongueShape);
            DrawFly(context);
        }
    }

    private static void DrawFly(DrawingContext context)
    {
        DrawRotatedEllipse(context, new Point(719.939, 172.537), 21.6235, 16.2176, -45);
        DrawOutlined(context, FlyWing, FlyBackWing, FlyOutlinePen);
        DrawOutlined(context, FlyWingLight, FlyFrontWing, FlyOutlinePen);
        context.DrawEllipse(Outline, null, new Point(697.368, 193.416), 23.279, 23.279);
        context.DrawEllipse(Brushes.White, FlyFacePen, new Point(703.857, 182.009), 12.15, 12.15);
        context.DrawEllipse(Brushes.White, FlyFacePen, new Point(684.428, 199.852), 12.15, 12.15);
        context.DrawGeometry(null, FlyFacePen, FlyEyeCrosses);
    }

    private static void DrawSleeping(DrawingContext context, double phase)
    {
        var breathe = Math.Sin(phase) * 5;

        context.DrawEllipse(ShadowBrush, null, new Point(DesignSize / 2, GroundY + 2), 31, 5);
        using (context.PushTransform(ArtToDesign))
        {
            DrawOutlined(context, Skin, LeftBackLeg);
            DrawOutlined(context, Skin, RightBackLeg);
            using (context.PushTransform(Matrix.CreateTranslation(0, breathe * 0.4)))
            {
                DrawOutlined(context, Skin, Body);
                context.DrawGeometry(Belly, null, BellyPatch);
            }

            DrawOutlined(context, Skin, SleepingLeftFrontLeg);
            DrawOutlined(context, Skin, SleepingRightFrontLeg);

            using (context.PushTransform(Matrix.CreateTranslation(0, SleepingHeadDrop + breathe)))
            {
                DrawOutlined(context, Skin, Head);
                context.DrawEllipse(Cheek, null, new Point(247, 287), 39, 30);
                context.DrawEllipse(Cheek, null, new Point(553, 287), 39, 30);
                DrawClosedEyes(context, ClosedEyes);
                context.DrawGeometry(null, FacePen, SleepingMouth);
            }
        }

        DrawZzz(context, ZzzPen, new Point(62, 40), phase);
    }

    private static void DrawOutlined(DrawingContext context, IBrush fill, Geometry geometry) =>
        DrawOutlined(context, fill, geometry, OutlinePen);

    private static void DrawOutlined(DrawingContext context, IBrush fill, Geometry geometry, IPen pen)
    {
        context.DrawGeometry(null, pen, geometry);
        context.DrawGeometry(fill, null, geometry);
    }

    private static void DrawRotatedEllipse(
        DrawingContext context,
        Point center,
        double radiusX,
        double radiusY,
        double angle)
    {
        var rotation = Matrix.CreateRotation(angle * Math.PI / 180) * Matrix.CreateTranslation(center.X, center.Y);
        using (context.PushTransform(rotation))
        {
            context.DrawEllipse(Outline, null, default, radiusX, radiusY);
        }
    }

    private static Geometry Parse(string pathData) => StreamGeometry.Parse(pathData);

    /// <summary>
    /// Body pose of a frame.
    /// </summary>
    /// <param name="OffsetX">Horizontal shift of the whole frog in the design space.</param>
    /// <param name="Lift">Height of the hop in the design space.</param>
    /// <param name="Stretch">How far the body is stretched up over the back legs, from 0 to 1.</param>
    /// <param name="Breathe">Vertical sway of the head in the art coordinates.</param>
    /// <param name="Crouch">How low the body is pressed to the ground in the art coordinates.</param>
    /// <param name="ArmsFolded">Whether the front legs are folded over the belly.</param>
    private readonly record struct Pose(
        double OffsetX,
        double Lift,
        double Stretch,
        double Breathe,
        double Crouch,
        bool ArmsFolded);
}
