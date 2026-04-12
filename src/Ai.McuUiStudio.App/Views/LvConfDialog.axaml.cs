using Avalonia.Controls;
using Ai.McuUiStudio.App.ViewModels;

namespace Ai.McuUiStudio.App.Views;

public partial class LvConfDialog : Window
{
    public LvConfDialog()
    {
        InitializeComponent();
    }

    private void SaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not LvConfDialogViewModel vm)
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
