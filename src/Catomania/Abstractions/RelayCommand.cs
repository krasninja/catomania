using System.Windows.Input;

namespace Catomania.Abstractions;

/// <summary>
/// Minimal <see cref="ICommand" /> implementation: the application uses click handlers and
/// commands only in the message box, a full MVVM framework would be an overkill.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    /// <summary>
    /// Initializes a new instance of the <see cref="RelayCommand" /> class.
    /// </summary>
    /// <param name="execute">Action to run when the command is executed.</param>
    /// <param name="canExecute">Predicate telling whether the command can be executed, always enabled when omitted.</param>
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    /// <inheritdoc />
    public void Execute(object? parameter) => _execute(parameter);

    /// <summary>
    /// Raise <see cref="CanExecuteChanged" /> so the bound controls requery <see cref="CanExecute" />.
    /// </summary>
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
