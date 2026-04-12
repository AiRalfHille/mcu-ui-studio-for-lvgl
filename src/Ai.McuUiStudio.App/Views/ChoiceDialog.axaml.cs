using Avalonia.Controls;

namespace Ai.McuUiStudio.App.Views;

public partial class ChoiceDialog : Window
{
    public ChoiceDialog()
    {
        InitializeComponent();
    }

    private void PrimaryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(ChoiceDialogResult.Primary);
    }

    private void SecondaryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(ChoiceDialogResult.Secondary);
    }

    private void CancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(ChoiceDialogResult.Cancel);
    }
}

public enum ChoiceDialogResult
{
    Cancel = 0,
    Primary = 1,
    Secondary = 2
}
