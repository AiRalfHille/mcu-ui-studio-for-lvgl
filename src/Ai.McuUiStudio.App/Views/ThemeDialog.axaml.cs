using Avalonia.Controls;
using Ai.McuUiStudio.App.ViewModels;

namespace Ai.McuUiStudio.App.Views;

public partial class ThemeDialog : Window
{
    public ThemeDialog()
    {
        InitializeComponent();
    }

    private void SaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ThemeDialogViewModel vm)
        {
            return;
        }

        if (vm.TrySave(out _))
        {
            Close(true);
        }
    }

    private void CancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }
}
