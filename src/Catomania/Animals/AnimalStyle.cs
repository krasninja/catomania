using Avalonia.Media;

namespace Catomania.Animals;

/// <summary>
/// The drawing style shared by all animals: the animals drawn with simple shapes use the same near-black
/// outlines and pink cheeks as the animals drawn from artwork, so all of them look alike.
/// </summary>
internal static class AnimalStyle
{
    /// <summary>
    /// Width of the outline of the body, the head and the limbs, in the design space. It matches the visible
    /// width of the outlines of the animals drawn from artwork.
    /// </summary>
    internal const double InkWidth = 1.3;

    /// <summary>
    /// The near-black of the outlines.
    /// </summary>
    internal static readonly Color InkColor = Color.FromUInt32(0xFF16141A);

    /// <summary>
    /// Brush of the outlines, the pupils, the nostrils and other dark details.
    /// </summary>
    internal static readonly IBrush Ink = new SolidColorBrush(InkColor);

    /// <summary>
    /// Pen of the outline of the body, the head and the limbs.
    /// </summary>
    internal static readonly IPen InkPen = new Pen(Ink, InkWidth, null, PenLineCap.Round, PenLineJoin.Round);

    /// <summary>
    /// Pen of the outline of small parts: paws, hooves, the snout.
    /// </summary>
    internal static readonly IPen ThinInkPen = new Pen(Ink, 1.2, null, PenLineCap.Round, PenLineJoin.Round);

    /// <summary>
    /// Pen of the facial lines: the mouth, the closed eyes, the whiskers.
    /// </summary>
    internal static readonly IPen FaceInkPen = new Pen(Ink, 1.1, null, PenLineCap.Round, PenLineJoin.Round);

    /// <summary>
    /// Brush of the pink cheeks.
    /// </summary>
    internal static readonly IBrush Cheek = new SolidColorBrush(Color.FromUInt32(0xCCF5A9A9));
}
