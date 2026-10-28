using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;

namespace Catomania.MessageBox;

/// <summary>
/// A small dialog window that shows a message with an icon and a set of buttons,
/// driven by <see cref="MessageBoxViewModel" />.
/// </summary>
public sealed partial class MessageBoxWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBoxWindow" /> class.
    /// Used by the XAML designer.
    /// </summary>
    public MessageBoxWindow()
    {
        InitializeComponent();

        DataContextChanged += (_, _) => AttachViewModel();

        // Focus the default button once the window is on screen, so Enter confirms the dialog.
        Opened += (_, _) => Dispatcher.UIThread.Post(
            FocusDefaultButton,
            DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageBoxWindow" /> class.
    /// </summary>
    /// <param name="viewModel">Message box view model.</param>
    public MessageBoxWindow(MessageBoxViewModel viewModel)
        : this()
    {
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    private void AttachViewModel()
    {
        if (DataContext is not MessageBoxViewModel viewModel)
        {
            return;
        }

        viewModel.CloseRequest -= OnViewModelCloseRequest;
        viewModel.CloseRequest += OnViewModelCloseRequest;
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        Closed += (_, _) =>
        {
            viewModel.CloseRequest -= OnViewModelCloseRequest;
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        };
        UpdateIcon(viewModel);
    }

    private void OnViewModelCloseRequest(object? sender, EventArgs e)
    {
        if (sender is MessageBoxViewModel viewModel)
        {
            Close(viewModel.ResultValue);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MessageBoxViewModel.Icon) && sender is MessageBoxViewModel viewModel)
        {
            UpdateIcon(viewModel);
        }
    }

    // The geometries come from the resource dictionary merged in XAML, which the XAML compiler
    // resolves at build time, unlike a ResourceInclude created in code that breaks trimming and AOT.
    private void UpdateIcon(MessageBoxViewModel viewModel)
    {
        var (key, brush) = viewModel.Icon switch
        {
            MessageBoxIcon.Question => ("FontAwesomeSolidCircleQuestion", Brushes.Blue),
            MessageBoxIcon.Warning => ("FontAwesomeSolidTriangleExclamation", Brushes.Yellow),
            MessageBoxIcon.Error => ("FontAwesomeRegularCircleXMark", Brushes.Red),
            _ => ("FontAwesomeSolidCircleInfo", Brushes.Blue),
        };

        if (TryGetResource(key, null, out var resource) && resource is Geometry geometry)
        {
            var iconPath = this.GetControl<Avalonia.Controls.Shapes.Path>("IconPath");
            iconPath.Data = geometry;
            iconPath.Fill = brush;
        }
    }

    private void FocusDefaultButton()
    {
        var buttons = this.GetLogicalDescendants()
            .OfType<Button>()
            .Where(b => b.IsVisible)
            .ToList();
        var button = buttons.FirstOrDefault(b => b.IsDefault) ?? buttons.FirstOrDefault();
        button?.Focus();
    }
}
