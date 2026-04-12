using Avalonia.Controls;
using Ai.McuUiStudio.App.ViewModels;

namespace Ai.McuUiStudio.App.Views;

public partial class TextInputDialog : Window
{
    public TextInputDialog()
    {
        InitializeComponent();
        Opened += HandleOpened;
    }

    private void HandleOpened(object? sender, EventArgs e)
    {
        this.FindControl<TextBox>("ValueTextBox")?.Focus();
    }

    private void ConfirmClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is TextInputDialogViewModel vm)
        {
            Close(vm.Value.Trim());
        }
    }

    private void CancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }
}
