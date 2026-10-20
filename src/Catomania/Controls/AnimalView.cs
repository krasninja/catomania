using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Catomania.Animals;
using Catomania.Models;

namespace Catomania.Controls;

/// <summary>
/// Draws an <see cref="Animal" />. The animal draws itself with vector shapes in a design space,
/// the view scales it to the control bounds and mirrors it when the animal faces left,
/// so it stays sharp at any size and no image assets are needed.
/// </summary>
public sealed class AnimalView : Control
{
    /// <summary>
    /// Defines the <see cref="Animal" /> property.
    /// </summary>
    public static readonly StyledProperty<Animal> AnimalProperty =
        AvaloniaProperty.Register<AnimalView, Animal>(nameof(Animal), new Cat());

    private AnimalRenderState _state = AnimalRenderState.Default;

    static AnimalView()
    {
        AffectsRender<AnimalView>(AnimalProperty);
    }

    /// <summary>
    /// Gets or sets the animal to draw.
    /// </summary>
    public Animal Animal
    {
        get => GetValue(AnimalProperty);
        set => SetValue(AnimalProperty, value);
    }

    /// <summary>
    /// Set the frame to draw and schedule a redraw.
    /// </summary>
    /// <param name="state">Render state produced by the simulation.</param>
    public void Update(AnimalRenderState state)
    {
        if (_state == state)
        {
            return;
        }

        _state = state;
        InvalidateVisual();
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        var size = Bounds.Size;
        if (size.Width <= 1 || size.Height <= 1)
        {
            return;
        }

        const double designSize = Animal.DesignSize;
        var scale = Math.Min(size.Width, size.Height) / designSize;
        var offsetX = (size.Width - (designSize * scale)) / 2;
        var offsetY = (size.Height - (designSize * scale)) / 2;
        var mirror = _state.FacingRight
            ? Matrix.Identity
            : Matrix.CreateScale(-1, 1) * Matrix.CreateTranslation(designSize, 0);
        var transform = mirror * Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(offsetX, offsetY);

        using (context.PushTransform(transform))
        {
            Animal.Render(context, _state);
        }
    }
}
