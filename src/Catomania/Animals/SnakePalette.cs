using Avalonia.Media;
using Catomania.Models;

namespace Catomania.Animals;

/// <summary>
/// Colors used to draw the snake. Instances are shared and created once.
/// </summary>
public sealed class SnakePalette
{
    private static readonly SnakePalette MintPalette = new(
        skin: 0xFF6DD3A0,
        spot: 0xFF4DB487,
        belly: 0xFFFFF0C2);

    private static readonly SnakePalette DarkGreenPalette = new(
        skin: 0xFF657A43,
        spot: 0xFF3E522E,
        belly: 0xFFB9AE78);

    private static readonly SnakePalette BluePalette = new(
        skin: 0xFF79BDE8,
        spot: 0xFF4C96C4,
        belly: 0xFFE3F2FA);

    private static readonly SnakePalette PinkPalette = new(
        skin: 0xFFF3A6C8,
        spot: 0xFFD477A4,
        belly: 0xFFFCE4EF);

    private static readonly SnakePalette YellowPalette = new(
        skin: 0xFFF2CE65,
        spot: 0xFFD5A33E,
        belly: 0xFFFFF0C2);

    private SnakePalette(uint skin, uint spot, uint belly)
    {
        SkinColor = Color.FromUInt32(skin);
        Skin = new SolidColorBrush(SkinColor);
        Spot = new SolidColorBrush(Color.FromUInt32(spot));
        Belly = new SolidColorBrush(Color.FromUInt32(belly));
        OutlineBrush = AnimalStyle.Ink;
        OutlinePen = AnimalStyle.InkPen;
        EyeOutlinePen = new Pen(OutlineBrush, 0.8);
        ClosedEyePen = new Pen(OutlineBrush, 1.3, null, PenLineCap.Round);
        MouthPen = AnimalStyle.FaceInkPen;
        RingOutlinePen = new Pen(OutlineBrush, 16 + (AnimalStyle.InkWidth * 2));
        RingPen = new Pen(Skin, 16);
    }

    /// <summary>
    /// Gets the main skin color.
    /// </summary>
    public Color SkinColor { get; }

    /// <summary>
    /// Gets the skin brush.
    /// </summary>
    public IBrush Skin { get; }

    /// <summary>
    /// Gets the spot brush.
    /// </summary>
    public IBrush Spot { get; }

    /// <summary>
    /// Gets the belly and chin brush.
    /// </summary>
    public IBrush Belly { get; }

    /// <summary>
    /// Gets the outline brush.
    /// </summary>
    public IBrush OutlineBrush { get; }

    /// <summary>
    /// Gets the head outline pen.
    /// </summary>
    public IPen OutlinePen { get; }

    /// <summary>
    /// Gets the eye outline pen.
    /// </summary>
    public IPen EyeOutlinePen { get; }

    /// <summary>
    /// Gets the closed eye pen.
    /// </summary>
    public IPen ClosedEyePen { get; }

    /// <summary>
    /// Gets the mouth pen.
    /// </summary>
    public IPen MouthPen { get; }

    /// <summary>
    /// Gets the outline pen of the sleeping ring.
    /// </summary>
    public IPen RingOutlinePen { get; }

    /// <summary>
    /// Gets the pen of the sleeping ring.
    /// </summary>
    public IPen RingPen { get; }

    /// <summary>
    /// Get the palette for the skin.
    /// </summary>
    /// <param name="skin">Snake skin.</param>
    /// <returns>Palette.</returns>
    public static SnakePalette FromSkin(SnakeSkin skin) => skin switch
    {
        SnakeSkin.DarkGreen => DarkGreenPalette,
        SnakeSkin.Blue => BluePalette,
        SnakeSkin.Pink => PinkPalette,
        SnakeSkin.Yellow => YellowPalette,
        _ => MintPalette,
    };
}
