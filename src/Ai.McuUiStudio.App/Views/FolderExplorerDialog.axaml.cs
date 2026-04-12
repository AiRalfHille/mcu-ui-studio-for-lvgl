using Avalonia.Controls;
using Avalonia.Interactivity;
using Ai.McuUiStudio.App.ViewModels;

namespace Ai.McuUiStudio.App.Views;

public partial class FolderExplorerDialog : Window
{
    private static string? s_lastSelectedPath;
    private static IReadOnlyCollection<string> s_lastExpandedPaths = Array.Empty<string>();

    public FolderExplorerDialog()
    {
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
            DataContext is FolderExplorerDialogViewModel vm)
        {
            vm.ExpandNode(node);
        }
    }

    private void SelectCurrentClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is FolderExplorerDialogViewModel vm && vm.SelectedDirectory?.FullPath is { } selectedPath)
        {
            Close(selectedPath);
        }
    }

    private void CancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private async void NewFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not FolderExplorerDialogViewModel vm)
        {
            return;
        }

        var dialog = new TextInputDialog
        {
            DataContext = new TextInputDialogViewModel(
                vm.NewFolderDialogTitle,
                vm.NewFolderDialogMessage,
                string.Empty,
                vm.NewFolderDialogConfirmLabel,
                vm.CancelLabel)
        };

        var folderName = await dialog.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return;
        }

        if (!vm.CreateFolder(folderName, out var createdPath, out var errorMessage))
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                var errorDialog = new InfoDialog
                {
                    DataContext = new InfoDialogViewModel(vm.NewFolderErrorDialogTitle, errorMessage, vm.CancelLabel)
                };

                await errorDialog.ShowDialog(this);
            }

            return;
        }

        vm.RestoreState(vm.SnapshotExpandedPaths(), createdPath);
    }

    private void HandleOpened(object? sender, EventArgs e)
    {
        if (DataContext is FolderExplorerDialogViewModel vm)
        {
            vm.RestoreState(s_lastExpandedPaths, s_lastSelectedPath);
        }
    }

    private void HandleClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is FolderExplorerDialogViewModel vm)
        {
            s_lastExpandedPaths = vm.SnapshotExpandedPaths();
            s_lastSelectedPath = vm.SelectedDirectory?.FullPath ?? vm.CurrentDirectoryPath;
        }
    }
}
