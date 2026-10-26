using Catomania.Abstractions;
using Catomania.Simulation;

namespace Catomania.Input;

/// <summary>
/// Tries the registered providers in order and returns the position from the first one that works.
/// </summary>
public sealed class CompositeCursorPositionProvider : ICursorPositionProvider, IDisposable
{
    private readonly IReadOnlyList<ICursorPositionProvider> _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeCursorPositionProvider" /> class.
    /// </summary>
    /// <param name="providers">Providers in the order of preference.</param>
    public CompositeCursorPositionProvider(IEnumerable<ICursorPositionProvider> providers)
    {
        _providers = providers.ToArray();
    }

    /// <inheritdoc />
    public string Name => string.Join(", ", _providers.Select(provider => provider.Name));

    /// <summary>
    /// Gets the name of the provider that has returned the position last time.
    /// </summary>
    public string? ActiveProviderName { get; private set; }

    /// <inheritdoc />
    public bool IsAvailable => _providers.Any(provider => provider.IsAvailable);

    /// <inheritdoc />
    public bool TryGetPosition(out ScreenPoint position)
    {
        foreach (var provider in _providers)
        {
            if (!provider.IsAvailable)
            {
                continue;
            }

            if (provider.TryGetPosition(out position))
            {
                ActiveProviderName = provider.Name;
                return true;
            }
        }

        ActiveProviderName = null;
        position = ScreenPoint.Zero;
        return false;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var provider in _providers.OfType<IDisposable>())
        {
            provider.Dispose();
        }
    }
}
