using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Catomania.Abstractions;

namespace Catomania.MessageBox;

/// <summary>
/// View model for message box.
/// </summary>
public sealed class MessageBoxViewModel : INotifyPropertyChanged, ICloseableViewModel, IDialogViewModel<DialogResult>
{
    private string _caption = "Information";
    private MessageBoxIcon _icon = MessageBoxIcon.Info;
    private bool _showOkButton = true;
    private bool _showYesButton;
    private bool _showNoButton;
    private bool _showCancelButton;
    private bool _showContinueButton;
    private bool _showAbortButton;
    private bool _showRetryButton;
    private bool _showIgnoreButton;

    /// <summary>
    /// Message box caption.
    /// </summary>
    public string Caption
    {
        get => _caption;
        set => Set(ref _caption, value);
    }

    /// <summary>
    /// Message to be displayed for user.
    /// </summary>
    public string Message { get; }

    /// <inheritdoc />
    public event EventHandler? CloseRequest;

    /// <inheritdoc />
    public DialogResult ResultValue { get; private set; } = DialogResult.OK;

    /// <inheritdoc />
    public DialogResult Result { get; private set; } = DialogResult.OK;

    /// <summary>
    /// Message box action selected.
    /// </summary>
    public RelayCommand ActionSelectCommand { get; }

    /// <summary>
    /// Copy message box text command.
    /// </summary>
    public RelayCommand CopyCommand { get; }

    /// <summary>
    /// Message box icon.
    /// </summary>
    public MessageBoxIcon Icon
    {
        get => _icon;
        set => Set(ref _icon, value);
    }

    /// <summary>
    /// Show "OK" button.
    /// </summary>
    public bool ShowOkButton
    {
        get => _showOkButton;
        set => Set(ref _showOkButton, value);
    }

    /// <summary>
    /// Show "Yes" button.
    /// </summary>
    public bool ShowYesButton
    {
        get => _showYesButton;
        set => Set(ref _showYesButton, value);
    }

    /// <summary>
    /// Show "No" button.
    /// </summary>
    public bool ShowNoButton
    {
        get => _showNoButton;
        set => Set(ref _showNoButton, value);
    }

    /// <summary>
    /// Show "Cancel" button.
    /// </summary>
    public bool ShowCancelButton
    {
        get => _showCancelButton;
        set => Set(ref _showCancelButton, value);
    }

    /// <summary>
    /// Show "Continue" button.
    /// </summary>
    public bool ShowContinueButton
    {
        get => _showContinueButton;
        set => Set(ref _showContinueButton, value);
    }

    /// <summary>
    /// Show "Abort" button.
    /// </summary>
    public bool ShowAbortButton
    {
        get => _showAbortButton;
        set => Set(ref _showAbortButton, value);
    }

    /// <summary>
    /// Show "Retry" button.
    /// </summary>
    public bool ShowRetryButton
    {
        get => _showRetryButton;
        set => Set(ref _showRetryButton, value);
    }

    /// <summary>
    /// Show "Ignore" button.
    /// </summary>
    public bool ShowIgnoreButton
    {
        get => _showIgnoreButton;
        set => Set(ref _showIgnoreButton, value);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="message">Message box text.</param>
    /// <param name="caption">Message box title.</param>
    public MessageBoxViewModel(string message, string? caption = null)
    {
        ActionSelectCommand = new RelayCommand(parameter => SetValueAndClose((DialogResult)parameter!));
        CopyCommand = new RelayCommand(_ => _ = CopyTextAsync());

        Caption = caption ?? Caption;
        Message = message;
    }

    /// <summary>
    /// Constructor to create content from exception.
    /// </summary>
    /// <param name="exception">Exception.</param>
    public MessageBoxViewModel(Exception exception)
        : this(GetMessageFromException(exception), "Error")
    {
        SetErrorMode();
    }

    private static string GetMessageFromException(Exception exception)
    {
        if (exception is TargetInvocationException { InnerException: { } } targetInvocationException
            && targetInvocationException.InnerException != null)
        {
            exception = targetInvocationException.InnerException;
        }
        return exception.Message;
    }

    private async Task CopyTextAsync()
    {
        var text = Message;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        var clipboard = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow?.Clipboard;
        if (clipboard == null)
        {
            return;
        }

        try
        {
            await clipboard.SetTextAsync(text);
        }
        catch (Exception)
        {
            // Ignore because it might produce unexpected exceptions.
        }
    }

    private void SetValueAndClose(DialogResult value)
    {
        ResultValue = value;
        Result = value;
        CloseRequest?.Invoke(this, EventArgs.Empty);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Show only "OK" button.
    /// </summary>
    /// <returns>Instance of <see cref="MessageBoxViewModel" />.</returns>
    public MessageBoxViewModel SetOkMode()
    {
        ShowOkButton = true;
        ShowCancelButton = false;
        ShowYesButton = false;
        ShowNoButton = false;
        ShowContinueButton = false;
        ShowAbortButton = false;
        ShowRetryButton = false;
        ShowIgnoreButton = false;
        Icon = MessageBoxIcon.Info;

        return this;
    }

    /// <summary>
    /// Show only "OK" and "Cancel" buttons.
    /// </summary>
    /// <returns>Instance of <see cref="MessageBoxViewModel" />.</returns>
    public MessageBoxViewModel SetOkCancelMode()
    {
        ShowOkButton = true;
        ShowCancelButton = true;
        ShowYesButton = false;
        ShowNoButton = false;
        ShowContinueButton = false;
        ShowAbortButton = false;
        ShowRetryButton = false;
        ShowIgnoreButton = false;
        Icon = MessageBoxIcon.Info;

        return this;
    }

    /// <summary>
    /// Show only "OK" button and error icon.
    /// </summary>
    /// <returns>Instance of <see cref="MessageBoxViewModel" />.</returns>
    public MessageBoxViewModel SetErrorMode()
    {
        ShowOkButton = true;
        ShowCancelButton = false;
        ShowYesButton = false;
        ShowNoButton = false;
        ShowContinueButton = false;
        ShowAbortButton = false;
        ShowRetryButton = false;
        ShowIgnoreButton = false;
        Icon = MessageBoxIcon.Error;

        return this;
    }

    /// <summary>
    /// Show only "Yes" and "No" buttons.
    /// </summary>
    /// <returns>Instance of <see cref="MessageBoxViewModel" />.</returns>
    public MessageBoxViewModel SetYesNoMode()
    {
        ShowOkButton = false;
        ShowCancelButton = false;
        ShowYesButton = true;
        ShowNoButton = true;
        ShowContinueButton = false;
        ShowAbortButton = false;
        ShowRetryButton = false;
        ShowIgnoreButton = false;
        Icon = MessageBoxIcon.Question;

        return this;
    }

    /// <summary>
    /// Show only "Yes", "No" and "Cancel" buttons.
    /// </summary>
    /// <returns>Instance of <see cref="MessageBoxViewModel" />.</returns>
    public MessageBoxViewModel SetYesNoCancelMode()
    {
        ShowOkButton = false;
        ShowCancelButton = true;
        ShowYesButton = true;
        ShowNoButton = true;
        ShowContinueButton = false;
        ShowAbortButton = false;
        ShowRetryButton = false;
        ShowIgnoreButton = false;
        Icon = MessageBoxIcon.Question;

        return this;
    }

    /// <summary>
    /// Show only "Abort", "Retry" and "Ignore" buttons.
    /// </summary>
    /// <returns>Instance of <see cref="MessageBoxViewModel" />.</returns>
    public MessageBoxViewModel SetAbortRetryIgnoreMode()
    {
        ShowOkButton = false;
        ShowCancelButton = false;
        ShowYesButton = false;
        ShowNoButton = false;
        ShowContinueButton = false;
        ShowAbortButton = true;
        ShowRetryButton = true;
        ShowIgnoreButton = true;
        Icon = MessageBoxIcon.Warning;

        return this;
    }
}
