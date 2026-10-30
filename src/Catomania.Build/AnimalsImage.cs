using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Catomania.Animals;
using Catomania.Models;

namespace Catomania.Build;

/// <summary>
/// Draws every animal standing on green grass into a single picture, used in the README.
/// The animals are drawn by the application's own vector rendering code, so the picture always
/// matches what users see on the screen.
/// </summary>
public static class AnimalsImage
{
    /// <summary>
    /// Number of rows the animals are arranged in.
    /// </summary>
    private const int Rows = 2;

    /// <summary>
    /// Horizontal gap between the animals and around them, in pixels.
    /// </summary>
    private const double Gap = 8;

    /// <summary>
    /// Margin above the first row, in pixels.
    /// </summary>
    private const double TopMargin = 8;

    /// <summary>
    /// Margin below the last row, in pixels.
    /// </summary>
    private const double BottomMargin = 12;

    /// <summary>
    /// Number of grass blades per pixel of the picture width.
    /// </summary>
    private const double BladesPerPixel = 0.9;

    /// <summary>
    /// Fixed seed of the grass blades, so the picture does not change between runs.
    /// </summary>
    private const int GrassSeed = 42;

    /// <summary>
    /// Size of an animal on the picture, in pixels: the medium size the application uses by default.
    /// </summary>
    private static readonly double AnimalSize = Catomania.Models.AnimalSize.Medium.ToLogicalSize();

    private static readonly Color GrassTopColor = Color.FromRgb(0x8C, 0xC8, 0x5A);
    private static readonly Color GrassBottomColor = Color.FromRgb(0x4E, 0x9A, 0x35);
    private static readonly Color[] BladeColors =
    [
        Color.FromRgb(0x3E, 0x82, 0x2A),
        Color.FromRgb(0x5D, 0xA8, 0x3E),
        Color.FromRgb(0x9F, 0xD4, 0x6A),
    ];

    /// <summary>
    /// Draw the animals and save the picture as a PNG file.
    /// </summary>
    /// <param name="path">Path of the PNG file to write.</param>
    public static void Create(string path)
    {
        EnsureAvalonia();

        var animals = CreateAnimals();
        var columns = (int)Math.Ceiling(animals.Count / (double)Rows);
        var width = (int)Math.Ceiling((columns * (AnimalSize + Gap)) + Gap);
        var height = (int)Math.Ceiling(TopMargin + (Rows * AnimalSize) + BottomMargin);

        using var bitmap = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        using (var context = bitmap.CreateDrawingContext())
        {
            DrawGrass(context, width, height);
            for (var i = 0; i < animals.Count; i++)
            {
                var row = i / columns;
                var column = i % columns;

                // A row that is not full is centered.
                var rowCount = Math.Min(columns, animals.Count - (row * columns));
                var rowOffset = (columns - rowCount) * (AnimalSize + Gap) / 2;
                var x = Gap + rowOffset + (column * (AnimalSize + Gap));
                var y = TopMargin + (row * AnimalSize);
                DrawAnimal(context, animals[i], new Point(x, y), facingRight: row == 0);
            }
        }

        bitmap.Save(path, PngBitmapEncoderOptions.Default);
    }

    /// <summary>
    /// Create one animal of every kind, the cat, the dog and the parrot once per color.
    /// </summary>
    /// <returns>Animals in the order they are drawn.</returns>
    private static List<Animal> CreateAnimals()
    {
        var animals = new List<Animal>();
        foreach (var kind in Enum.GetValues<AnimalKind>())
        {
            switch (kind)
            {
                case AnimalKind.Cat:
                    animals.AddRange(Enum.GetValues<CatColorScheme>().Select(scheme => new Cat(scheme)));
                    break;
                case AnimalKind.Dog:
                    animals.AddRange(Enum.GetValues<DogCoat>().Select(coat => new Dog(coat)));
                    break;
                case AnimalKind.Parrot:
                    animals.AddRange(Enum.GetValues<ParrotPlumage>().Select(plumage => new Parrot(plumage)));
                    break;
                default:
                    animals.Add(Animal.Create(new AppSettings { Animal = kind }));
                    break;
            }
        }

        return animals;
    }

    /// <summary>
    /// Start the headless Avalonia platform with the Skia renderer. Drawing needs a platform,
    /// but no windows: the picture is rendered straight into a bitmap.
    /// </summary>
    private static void EnsureAvalonia()
    {
        if (Application.Current != null)
        {
            return;
        }

        AppBuilder.Configure<Application>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions
            {
                UseHeadlessDrawing = false,
            })
            .SetupWithoutStarting();
    }

    private static void DrawGrass(DrawingContext context, int width, int height)
    {
        var background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(GrassTopColor, 0),
                new GradientStop(GrassBottomColor, 1),
            },
        };
        context.FillRectangle(background, new Rect(0, 0, width, height));

        var random = new Random(GrassSeed);
        var pens = BladeColors.Select(color => new Pen(new SolidColorBrush(color), 1.4, lineCap: PenLineCap.Round)).ToArray();
        var bladeCount = (int)(width * BladesPerPixel);
        for (var i = 0; i < bladeCount; i++)
        {
            var root = new Point(random.NextDouble() * width, random.NextDouble() * (height + 10));
            var length = 5 + (random.NextDouble() * 9);
            var lean = (random.NextDouble() - 0.5) * 7;
            var tip = new Point(root.X + lean, root.Y - length);
            var bend = new Point(root.X + (lean * 0.2), root.Y - (length * 0.6));

            var geometry = new StreamGeometry();
            using (var geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(root, false);
                geometryContext.QuadraticBezierTo(bend, tip);
                geometryContext.EndFigure(false);
            }

            context.DrawGeometry(null, pens[random.Next(pens.Length)], geometry);
        }
    }

    private static void DrawAnimal(DrawingContext context, Animal animal, Point origin, bool facingRight)
    {
        // The same transform AnimalView applies: mirror in the design space, then scale and move.
        const double designSize = Animal.DesignSize;
        var scale = AnimalSize / designSize;
        var mirror = facingRight
            ? Matrix.Identity
            : Matrix.CreateScale(-1, 1) * Matrix.CreateTranslation(designSize, 0);
        var transform = mirror * Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(origin.X, origin.Y);

        using (context.PushTransform(transform))
        {
            animal.Render(context, new AnimalRenderState(AnimalAnimationState.Idle, facingRight, 0, false));
        }
    }
}
