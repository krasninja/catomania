using Avalonia.Media;
using Catomania.Models;

namespace Catomania.Animals;

/// <summary>
/// Colors of the patches on the dog's white coat, with the eyes and nose that go with them.
/// Instances are shared and created once.
/// </summary>
public sealed class DogPalette
{
    // The iris must stand apart from the patches and from where they blend into the outline and the coat,
    // the blink is checked by the iris color disappearing.
    private static readonly DogPalette LiverPalette = new(
        0xFF6B3A26, 0xFF552C1C, 0xFF45231A, 0xFFB8741C, 0xFF1E120C, 0xFF3A1F16);

    private static readonly DogPalette BlackPalette = new(
        0xFF2E2E34, 0xFF222227, 0xFF111114, 0xFF8C5519, 0xFF111114, 0xFF1A1A1E);

    private static readonly DogPalette RedPalette = new(
        0xFFB5612F, 0xFF964D24, 0xFF7A3C1B, 0xFFC0A808, 0xFF2A160C, 0xFF4A2A1C);

    private static readonly DogPalette LemonPalette = new(
        0xFFE3B26A, 0xFFC99A55, 0xFFA8783C, 0xFFC0A808, 0xFF2A160C, 0xFF6B4A36);

    private DogPalette(uint patch, uint patchFar, uint patchDark, uint iris, uint pupil, uint nose)
    {
        PatchColor = Color.FromUInt32(patch);
        IrisColor = Color.FromUInt32(iris);
        Patch = new SolidColorBrush(PatchColor);
        PatchFar = new SolidColorBrush(Color.FromUInt32(patchFar));
        Iris = new SolidColorBrush(IrisColor);
        Pupil = new SolidColorBrush(Color.FromUInt32(pupil));
        Nose = new SolidColorBrush(Color.FromUInt32(nose));

        OutlinePen = AnimalStyle.InkPen;
        ThinOutlinePen = AnimalStyle.ThinInkPen;
        MouthPen = AnimalStyle.FaceInkPen;
        StrandPen = new Pen(new SolidColorBrush(Color.FromUInt32(patchDark)), 1, null, PenLineCap.Round);
        TailStripePen = new Pen(Patch, 2.4, null, PenLineCap.Round);
    }

    /// <summary>
    /// Gets the color of the patches.
    /// </summary>
    public Color PatchColor { get; }

    /// <summary>
    /// Gets the iris color.
    /// </summary>
    public Color IrisColor { get; }

    /// <summary>
    /// Gets the brush of the patches: the saddle, the head and the ears.
    /// </summary>
    public IBrush Patch { get; }

    /// <summary>
    /// Gets the brush of the patches on the far legs, in the shade of the body.
    /// </summary>
    public IBrush PatchFar { get; }

    /// <summary>
    /// Gets the iris brush.
    /// </summary>
    public IBrush Iris { get; }

    /// <summary>
    /// Gets the pupil brush.
    /// </summary>
    public IBrush Pupil { get; }

    /// <summary>
    /// Gets the nose brush, also used for the open mouth.
    /// </summary>
    public IBrush Nose { get; }

    /// <summary>
    /// Gets the body outline pen.
    /// </summary>
    public IPen OutlinePen { get; }

    /// <summary>
    /// Gets the thin outline pen of the paws and the belly fringe.
    /// </summary>
    public IPen ThinOutlinePen { get; }

    /// <summary>
    /// Gets the mouth and tongue pen.
    /// </summary>
    public IPen MouthPen { get; }

    /// <summary>
    /// Gets the pen of the strands on the ears and the brow.
    /// </summary>
    public IPen StrandPen { get; }

    /// <summary>
    /// Gets the pen of the stripe along the tail.
    /// </summary>
    public IPen TailStripePen { get; }

    /// <summary>
    /// Get the palette for the coat.
    /// </summary>
    /// <param name="coat">Coat color.</param>
    /// <returns>Palette.</returns>
    public static DogPalette FromCoat(DogCoat coat) => coat switch
    {
        DogCoat.Black => BlackPalette,
        DogCoat.Red => RedPalette,
        DogCoat.Lemon => LemonPalette,
        _ => LiverPalette,
    };
}
