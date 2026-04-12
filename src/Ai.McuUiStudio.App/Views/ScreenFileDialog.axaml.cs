using Avalonia.Controls;
using Avalonia.Interactivity;
using Ai.McuUiStudio.App.ViewModels;

namespace Ai.McuUiStudio.App.Views;

public partial class ScreenFileDialog : Window
{
    private static string? s_lastSelectedDirectoryPath;
    private static IReadOnlyCollection<string> s_lastExpandedPaths = Array.Empty<string>();
    private readonly bool _createMode;

    public ScreenFileDialog() : this(false)
    {
    }

    public ScreenFileDialog(bool createMode)
    {
        _createMode = createMode;
        InitializeComponent();

        var treeView = this.FindControl<TreeView>("DirectoryTreeView");
        if (treeView is not null)
        {
            treeView.AddHandler(TreeViewItem.ExpandedEvent, DirectoryTreeItemExpanded);
        }

        Opened += HandleOpened;
        Closing += HandleClosing;
    }

    private void DirectoryTreeItemExpanded(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TreeViewItem treeViewItem &&
            treeViewItem.DataContext is DirectoryTreeNodeViewModel node &&
            DataContext is ScreenFileDialogViewModel vm)
        {
            vm.ExpandNode(node);
        }
    }

    private async void ConfirmClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not ScreenFileDialogViewModel vm)
        {
            return;
        }

        if (_createMode)
        {
            if (!vm.TryCreateFilePathFromInput(out var targetPath, out var errorMessage))
            {
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    var dialog = new InfoDialog
                    {
                        DataContext = new InfoDialogViewModel(vm.ErrorDialogTitle, errorMessage, vm.CancelLabel)
                    };
                    await dialog.ShowDialog(this);
                }

                return;
            }

            Close(targetPath);
            return;
        }

        var selectedPath = vm.GetSelectedFilePath();
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            Close(selectedPath);
        }
    }

    private void CancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private void HandleOpened(object? sender, EventArgs e)
    {
        if (DataContext is ScreenFileDialogViewModel vm)
        {
            vm.RestoreState(s_lastExpandedPaths, s_lastSelectedDirectoryPath);
        }
    }

    private void HandleClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is ScreenFileDialogViewModel vm)
        {
            s_lastExpandedPaths = vm.SnapshotExpandedPaths();
            s_lastSelectedDirectoryPath = vm.SelectedDirectory?.FullPath ?? vm.CurrentDirectoryPath;
        }
    }
}
