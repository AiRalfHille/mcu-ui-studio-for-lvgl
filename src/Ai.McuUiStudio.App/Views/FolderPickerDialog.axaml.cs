using Avalonia.Controls;
using Avalonia.Input;
using Ai.McuUiStudio.App.ViewModels;

namespace Ai.McuUiStudio.App.Views;

public partial class FolderPickerDialog : Window
{
    public FolderPickerDialog()
    {
        InitializeComponent();
    }

    private void GoClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is FolderPickerDialogViewModel vm)
        {
            vm.NavigateToInput();
        }
    }

    private void UpClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is FolderPickerDialogViewModel vm)
        {
            vm.NavigateUp();
        }
    }

    private void HomeClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is FolderPickerDialogViewModel vm)
        {
            vm.NavigateHome();
        }
    }

    private void OpenClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is FolderPickerDialogViewModel vm && vm.NavigateToSelected())
        {
            var listBox = this.FindControl<ListBox>("FolderListBox");
            if (listBox is not null)
            {
                listBox.SelectedItem = null;
            }
        }
    }

    private void SelectCurrentClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is FolderPickerDialogViewModel vm)
        {
            Close(vm.CurrentPath);
        }
    }

    private void CancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(null);
    }

    private void FolderListDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is FolderPickerDialogViewModel vm && vm.NavigateToSelected())
        {
            var listBox = this.FindControl<ListBox>("FolderListBox");
            if (listBox is not null)
            {
                listBox.SelectedItem = null;
            }
        }
    }
}
