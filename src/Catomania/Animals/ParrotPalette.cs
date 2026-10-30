using Avalonia.Media;
using Catomania.Models;

namespace Catomania.Animals;

/// <summary>
/// Colors used to draw the parrot, one palette per macaw species. Instances are shared and created once.
/// </summary>
public sealed class ParrotPalette
{
    private static readonly ParrotPalette ScarletPalette = new(
        plumage: 0xFFE0342F,
        chest: 0xFFE0342F,
        forehead: null,
        wingBands: [0xFFE0342F, 0xFFFFC83D, 0xFF3FAE5A, 0xFF2F7BD8],
        tailTip: 0xFF2F7BD8,
        face: 0xFFFFF6EC,
        faceLines: 0xFFE8A0A0,
        upperBeak: 0xFFF0E2C8,
        lowerBeak: 0xFF2E2A2A,
        iris: 0xFFFAE278,
        outline: 0xFF5A1414);

    private static readonly ParrotPalette BlueAndGoldPalette = new(
        plumage: 0xFF2F86D8,
        chest: 0xFFF9C23C,
        forehead: 0xFF4CB860,
        wingBands: [0xFF2F86D8, 0xFF56A6EE, 0xFF2F86D8, 0xFF1F5FB0],
        tailTip: 0xFF1F5FB0,
        face: 0xFFFFF8F0,
        faceLines: 0xFF3A3A3A,
        upperBeak: 0xFF34343A,
        lowerBeak: 0xFF1E1E22,
        iris: 0xFFFAE278,
        outline: 0xFF1A3A66);

    private static readonly ParrotPalette GreenPalette = new(
        plumage: 0xFF5BAF3E,
        chest: 0xFF6CBF4A,
        forehead: 0xFFE0342F,
        wingBands: [0xFF5BAF3E, 0xFF7CCB5A, 0xFF4F9E3A, 0xFF2F7BD8],
        tailTip: 0xFF2F7BD8,
        face: 0xFFFFEFEA,
        faceLines: 0xFF6A6A6A,
        upperBeak: 0xFF3A3A3E,
        lowerBeak: 0xFF242428,
        iris: 0xFFFAE278,
        outline: 0xFF24461A);

    // The hyacinth has no bare face, the yellow around the pupil stands for its bright eye ring.
    private static readonly ParrotPalette HyacinthPalette = new(
        plumage: 0xFF2B4FB8,
        chest: 0xFF2B4FB8,
        forehead: null,
        wingBands: [0xFF2B4FB8, 0xFF3A62CC, 0xFF2B4FB8, 0xFF1F3C94],
        tailTip: 0xFF1F3C94,
        face: 0xFF2B4FB8,
        faceLines: 0x00000000,
        upperBeak: 0xFF2E2E32,
        lowerBeak: 0xFF1E1E22,
        iris: 0xFFF5C83A,
        outline: 0xFF15255A);

    private ParrotPalette(
        uint plumage,
        uint chest,
        uint? forehead,
        uint[] wingBands,
        uint tailTip,
        uint face,
        uint faceLines,
        uint upperBeak,
        uint lowerBeak,
        uint iris,
        uint outline)
    {
        PlumageColor = Color.FromUInt32(plumage);
        IrisColor = Color.FromUInt32(iris);
        Plumage = new SolidColorBrush(PlumageColor);
        Chest = new SolidColorBrush(Color.FromUInt32(chest));
        Forehead = forehead is { } foreheadColor ? new SolidColorBrush(Color.FromUInt32(foreheadColor)) : null;
        Coverts = new SolidColorBrush(Color.FromUInt32(wingBands[0]));
        UpperWingBand = new SolidColorBrush(Color.FromUInt32(wingBands[1]));
        LowerWingBand = new SolidColorBrush(Color.FromUInt32(wingBands[2]));
        FlightFeathers = new SolidColorBrush(Color.FromUInt32(wingBands[3]));
        TailTip = new SolidColorBrush(Color.FromUInt32(tailTip));
        Face = new SolidColorBrush(Color.FromUInt32(face));
        UpperBeak = new SolidColorBrush(Color.FromUInt32(upperBeak));
        LowerBeak = new SolidColorBrush(Color.FromUInt32(lowerBeak));
        Iris = new SolidColorBrush(IrisColor);
        FaceLinePen = new Pen(new SolidColorBrush(Color.FromUInt32(faceLines)), 0.8, null, PenLineCap.Round);
        OutlinePen = new Pen(new SolidColorBrush(Color.FromUInt32(outline)), 1.3, null, PenLineCap.Round, PenLineJoin.Round);
    }

    /// <summary>
    /// Gets the main plumage color of the body, the head and the tail.
    /// </summary>
    public Color PlumageColor { get; }

    /// <summary>
    /// Gets the iris color.
    /// </summary>
    public Color IrisColor { get; }

    /// <summary>
    /// Gets the plumage brush.
    /// </summary>
    public IBrush Plumage { get; }

    /// <summary>
    /// Gets the chest and belly brush.
    /// </summary>
    public IBrush Chest { get; }

    /// <summary>
    /// Gets the forehead patch brush, null when the forehead is the plumage color.
    /// </summary>
    public IBrush? Forehead { get; }

    /// <summary>
    /// Gets the brush of the coverts, the feathers on the wing at the shoulder.
    /// </summary>
    public IBrush Coverts { get; }

    /// <summary>
    /// Gets the brush of the band below the coverts.
    /// </summary>
    public IBrush UpperWingBand { get; }

    /// <summary>
    /// Gets the brush of the band above the flight feathers.
    /// </summary>
    public IBrush LowerWingBand { get; }

    /// <summary>
    /// Gets the brush of the flight feathers at the end of the wing.
    /// </summary>
    public IBrush FlightFeathers { get; }

    /// <summary>
    /// Gets the brush of the tail tip.
    /// </summary>
    public IBrush TailTip { get; }

    /// <summary>
    /// Gets the brush of the bare face around the eye.
    /// </summary>
    public IBrush Face { get; }

    /// <summary>
    /// Gets the upper beak brush.
    /// </summary>
    public IBrush UpperBeak { get; }

    /// <summary>
    /// Gets the lower beak brush.
    /// </summary>
    public IBrush LowerBeak { get; }

    /// <summary>
    /// Gets the iris brush.
    /// </summary>
    public IBrush Iris { get; }

    /// <summary>
    /// Gets the pen of the rows of tiny feathers on the face.
    /// </summary>
    public IPen FaceLinePen { get; }

    /// <summary>
    /// Gets the body outline pen.
    /// </summary>
    public IPen OutlinePen { get; }

    /// <summary>
    /// Get the palette for the plumage.
    /// </summary>
    /// <param name="plumage">Plumage.</param>
    /// <returns>Palette.</returns>
    public static ParrotPalette FromPlumage(ParrotPlumage plumage) => plumage switch
    {
        ParrotPlumage.BlueAndGold => BlueAndGoldPalette,
        ParrotPlumage.Green => GreenPalette,
        ParrotPlumage.Hyacinth => HyacinthPalette,
        _ => ScarletPalette,
    };
}
