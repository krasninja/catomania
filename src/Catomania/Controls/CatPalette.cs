using Avalonia.Media;
using Catomania.Models;

namespace Catomania.Controls;

/// <summary>
/// Colors used to draw the cat. Instances are shared and created once.
/// </summary>
public sealed class CatPalette
{
    private static readonly CatPalette GingerPalette = new(
        0xFFE89B4E, 0xFFC4762F, 0xFFF8E6CC, 0xFFE9A0A6, 0xFFD9788A, 0xFF6FA644, 0xFF2C2622, 0xFFFFF3E2);

    private static readonly CatPalette GrayPalette = new(
        0xFF9BA5B0, 0xFF78838F, 0xFFE6EAEE, 0xFFD3A0A8, 0xFFC98894, 0xFFC9A227, 0xFF23262B, 0xFFF2F5F8);

    private static readonly CatPalette BlackPalette = new(
        0xFF3B3B44, 0xFF26262D, 0xFF5C5C68, 0xFF6E4A55, 0xFFA86A78, 0xFF86C05A, 0xFF14141A, 0xFFB9B9C6);

    private static readonly CatPalette CreamPalette = new(
        0xFFF1E7D6, 0xFFDCCDB4, 0xFFFFFBF3, 0xFFE7AEB4, 0xFFDE93A0, 0xFF5B93C9, 0xFF3A3330, 0xFFFFFFFF);

    private CatPalette(uint fur, uint furDark, uint belly, uint earInner, uint nose, uint eye, uint pupil, uint whisker)
    {
        FurColor = Color.FromUInt32(fur);
        FurDarkColor = Color.FromUInt32(furDark);
        EyeColor = Color.FromUInt32(eye);
        Fur = new SolidColorBrush(FurColor);
        FurDark = new SolidColorBrush(FurDarkColor);
        Belly = new SolidColorBrush(Color.FromUInt32(belly));
        EarInner = new SolidColorBrush(Color.FromUInt32(earInner));
        Nose = new SolidColorBrush(Color.FromUInt32(nose));
        Eye = new SolidColorBrush(EyeColor);
        Pupil = new SolidColorBrush(Color.FromUInt32(pupil));
        Whisker = new SolidColorBrush(Color.FromUInt32(whisker));
        OutlinePen = new Pen(FurDark, 1.4);
        TailPen = new Pen(Fur, 8, null, PenLineCap.Round);
        TailTipPen = new Pen(FurDark, 8, null, PenLineCap.Round);
        StripePen = new Pen(FurDark, 3, null, PenLineCap.Round);
        WhiskerPen = new Pen(Whisker, 0.9, null, PenLineCap.Round);
        FacePen = new Pen(Pupil, 1.1, null, PenLineCap.Round);
        ZzzPen = new Pen(Whisker, 1.8, null, PenLineCap.Round, PenLineJoin.Round);
        ShadowBrush = new SolidColorBrush(Color.FromUInt32(0x28000000));
    }

    /// <summary>
    /// Gets the fur color.
    /// </summary>
    public Color FurColor { get; }

    /// <summary>
    /// Gets the dark fur color, used for stripes and outlines.
    /// </summary>
    public Color FurDarkColor { get; }

    /// <summary>
    /// Gets the eye color.
    /// </summary>
    public Color EyeColor { get; }

    /// <summary>
    /// Gets the fur brush.
    /// </summary>
    public IBrush Fur { get; }

    /// <summary>
    /// Gets the dark fur brush.
    /// </summary>
    public IBrush FurDark { get; }

    /// <summary>
    /// Gets the belly brush.
    /// </summary>
    public IBrush Belly { get; }

    /// <summary>
    /// Gets the inner ear brush.
    /// </summary>
    public IBrush EarInner { get; }

    /// <summary>
    /// Gets the nose brush.
    /// </summary>
    public IBrush Nose { get; }

    /// <summary>
    /// Gets the iris brush.
    /// </summary>
    public IBrush Eye { get; }

    /// <summary>
    /// Gets the pupil and mouth lines brush.
    /// </summary>
    public IBrush Pupil { get; }

    /// <summary>
    /// Gets the whiskers brush.
    /// </summary>
    public IBrush Whisker { get; }

    /// <summary>
    /// Gets the shadow brush drawn under the cat.
    /// </summary>
    public IBrush ShadowBrush { get; }

    /// <summary>
    /// Gets the body outline pen.
    /// </summary>
    public IPen OutlinePen { get; }

    /// <summary>
    /// Gets the tail pen.
    /// </summary>
    public IPen TailPen { get; }

    /// <summary>
    /// Gets the dark tail tip pen.
    /// </summary>
    public IPen TailTipPen { get; }

    /// <summary>
    /// Gets the stripes pen.
    /// </summary>
    public IPen StripePen { get; }

    /// <summary>
    /// Gets the whiskers pen.
    /// </summary>
    public IPen WhiskerPen { get; }

    /// <summary>
    /// Gets the nose and mouth pen.
    /// </summary>
    public IPen FacePen { get; }

    /// <summary>
    /// Gets the pen used to draw the sleeping "zzz".
    /// </summary>
    public IPen ZzzPen { get; }

    /// <summary>
    /// Get the palette for the color scheme.
    /// </summary>
    /// <param name="scheme">Color scheme.</param>
    /// <returns>Palette.</returns>
    public static CatPalette FromScheme(CatColorScheme scheme) => scheme switch
    {
        CatColorScheme.Gray => GrayPalette,
        CatColorScheme.Black => BlackPalette,
        CatColorScheme.Cream => CreamPalette,
        _ => GingerPalette,
    };
}
