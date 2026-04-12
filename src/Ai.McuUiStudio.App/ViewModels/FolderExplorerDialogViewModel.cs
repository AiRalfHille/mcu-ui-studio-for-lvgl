using System.Collections.ObjectModel;
using System.IO;
using Ai.McuUiStudio.App.Services.Localization;

namespace Ai.McuUiStudio.App.ViewModels;

public sealed class FolderExplorerDialogViewModel : ViewModelBase
{
    private readonly LocalizationCatalog _localizationCatalog;
    private DirectoryTreeNodeViewModel? _selectedDirectory;
    private string _currentDirectoryPath = string.Empty;
    private string _errorText = string.Empty;
    private readonly string? _initialPath;

    public FolderExplorerDialogViewModel(LocalizationCatalog localizationCatalog, string title, string? initialPath)
    {
        _localizationCatalog = localizationCatalog;
        Title = string.IsNullOrWhiteSpace(title) ? Ui("dialog.folder_explorer_title") : title;
        RootNodes = [];
        FileEntries = [];
        _initialPath = NormalizeDirectory(initialPath);

        var computerNode = new DirectoryTreeNodeViewModel(Ui("dialog.folder_explorer.computer"), null, isVirtual: true);
        foreach (var driveRoot in EnumerateDriveRoots().OrderBy(path => GetDisplayName(path), StringComparer.OrdinalIgnoreCase))
        {
            computerNode.Children.Add(CreateDirectoryNode(driveRoot, Ui("dialog.file.bytes")));
        }

        RootNodes.Add(computerNode);
        computerNode.IsExpanded = true;

        if (!string.IsNullOrWhiteSpace(_initialPath))
        {
            SelectClosestNode(_initialPath);
        }
        else if (computerNode.Children.Count > 0)
        {
            SelectedDirectory = computerNode.Children[0];
        }
    }

    public string Title { get; }

    private string Ui(string key) => _localizationCatalog.GetUiString(key);

    public ObservableCollection<DirectoryTreeNodeViewModel> RootNodes { get; }

    public ObservableCollection<FileEntryViewModel> FileEntries { get; }

    public DirectoryTreeNodeViewModel? SelectedDirectory
    {
        get => _selectedDirectory;
        set
        {
            if (!SetProperty(ref _selectedDirectory, value))
            {
                return;
            }

            if (value is not null && !value.IsVirtual)
            {
                value.EnsureChildrenLoaded();
                CurrentDirectoryPath = value.FullPath ?? string.Empty;
                RefreshFiles(value.FullPath);
            }

            RaisePropertyChanged(nameof(CanSelectCurrentDirectory));
            RaisePropertyChanged(nameof(CanCreateFolder));
            RaisePropertyChanged(nameof(SelectedDirectoryStatusText));
        }
    }

    public string CurrentDirectoryPath
    {
        get => _currentDirectoryPath;
        private set => SetProperty(ref _currentDirectoryPath, value);
    }

    public string ErrorText
    {
        get => _errorText;
        private set => SetProperty(ref _errorText, value);
    }

    public string SelectCurrentLabel => Ui("dialog.folder_explorer.select_current");

    public string NewFolderLabel => Ui("dialog.folder_explorer.new_folder");

    public string CancelLabel => Ui("dialog.close");

    public string NewFolderDialogTitle => Ui("dialog.folder_explorer.new_folder_title");

    public string NewFolderDialogMessage => Ui("dialog.folder_explorer.new_folder_message");

    public string NewFolderDialogConfirmLabel => Ui("dialog.folder_explorer.new_folder_confirm");

    public string NewFolderErrorDialogTitle => Ui("dialog.folder_explorer.new_folder_error_title");

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

    public bool CanSelectCurrentDirectory => SelectedDirectory is { IsVirtual: false, FullPath: not null };

    public bool CanCreateFolder => CanSelectCurrentDirectory;

    public string SelectedDirectoryStatusText => DescribeDirectory(CurrentDirectoryPath);

    public void ExpandNode(DirectoryTreeNodeViewModel? node)
    {
        node?.EnsureChildrenLoaded();
    }

    public void RestoreState(IReadOnlyCollection<string>? expandedPaths, string? selectedPath)
    {
        foreach (var rootNode in RootNodes)
        {
            rootNode.RestoreExpandedPaths(expandedPaths);
        }

        var targetPath = NormalizeDirectory(selectedPath) ?? _initialPath;
        if (!string.IsNullOrWhiteSpace(targetPath))
        {
            SelectClosestNode(targetPath);
        }
    }

    public IReadOnlyCollection<string> SnapshotExpandedPaths()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rootNode in RootNodes)
        {
            rootNode.CollectExpandedPaths(result);
        }

        return result.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public bool CreateFolder(string folderName, out string? createdPath, out string? errorMessage)
    {
        createdPath = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(CurrentDirectoryPath))
        {
            errorMessage = Ui("error.folder_explorer.no_target");
            return false;
        }

        if (string.IsNullOrWhiteSpace(folderName))
        {
            errorMessage = Ui("error.folder_explorer.enter_name");
            return false;
        }

        if (folderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            errorMessage = Ui("error.folder_explorer.invalid_name");
            return false;
        }

        var targetPath = Path.Combine(CurrentDirectoryPath, folderName.Trim());
        if (Directory.Exists(targetPath))
        {
            errorMessage = string.Format(
                Ui("error.folder_explorer.exists"),
                folderName);
            return false;
        }

        try
        {
            Directory.CreateDirectory(targetPath);
            RefreshSelectedDirectory();
            createdPath = targetPath;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public void RefreshSelectedDirectory()
    {
        if (SelectedDirectory is null || SelectedDirectory.IsVirtual)
        {
            return;
        }

        SelectedDirectory.ResetChildren();
        SelectedDirectory.EnsureChildrenLoaded();
        RefreshFiles(SelectedDirectory.FullPath);
        RaisePropertyChanged(nameof(SelectedDirectoryStatusText));
    }

    private void RefreshFiles(string? directoryPath)
    {
        FileEntries.Clear();

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            ErrorText = string.Empty;
            RaisePropertyChanged(nameof(HasError));
            return;
        }

        try
        {
            foreach (var filePath in Directory.EnumerateFiles(directoryPath)
                         .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(filePath);
                FileEntries.Add(new FileEntryViewModel(fileName, filePath, Ui("dialog.file.bytes")));
            }

            ErrorText = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }

        RaisePropertyChanged(nameof(HasError));
        RaisePropertyChanged(nameof(SelectedDirectoryStatusText));
    }

    private void SelectClosestNode(string startPath)
    {
        foreach (var rootNode in RootNodes)
        {
            var match = rootNode.FindOrExpandTo(startPath);
            if (match is not null)
            {
                SelectedDirectory = match;
                return;
            }
        }
    }

    private static IEnumerable<string> EnumerateDriveRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                var root = NormalizeDirectory(drive.RootDirectory.FullName);
                if (!string.IsNullOrWhiteSpace(root))
                {
                    roots.Add(root);
                }
            }
            catch
            {
            }
        }

        var unixRoots = new List<string>();
        var rootPath = NormalizeDirectory(Path.DirectorySeparatorChar.ToString());
        if (!string.IsNullOrWhiteSpace(rootPath))
        {
            unixRoots.Add(rootPath);
        }

        var home = NormalizeDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (!string.IsNullOrWhiteSpace(home) &&
            !unixRoots.Contains(home, StringComparer.OrdinalIgnoreCase))
        {
            unixRoots.Add(home);
        }

        foreach (var unixRoot in unixRoots)
        {
            roots.Add(unixRoot);
        }

        return roots.OrderBy(path => path, StringComparer.OrdinalIgnoreCase);
    }

    private static DirectoryTreeNodeViewModel CreateDirectoryNode(string directoryPath, string bytesLabel)
    {
        var displayName = GetDisplayName(directoryPath);
        var node = new DirectoryTreeNodeViewModel(displayName, directoryPath, bytesLabel: bytesLabel);
        node.PreparePlaceholder();
        return node;
    }

    private static string GetDisplayName(string path)
    {
        var fileName = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        return string.IsNullOrWhiteSpace(fileName) ? path : fileName;
    }

    private static string? NormalizeDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            return Path.GetFullPath(path.Trim());
        }
        catch
        {
            return null;
        }
    }

    private string DescribeDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return Ui("dialog.folder_explorer.status.none");
        }

        try
        {
            var projectFiles = Directory.GetFiles(path, "*.lvglproj", SearchOption.TopDirectoryOnly);
            if (projectFiles.Length == 1)
            {
                return Ui("dialog.folder_explorer.status.project_found");
            }

            if (projectFiles.Length > 1)
            {
                return Ui("dialog.folder_explorer.status.project_multiple");
            }

            var hasVisibleEntries = Directory.EnumerateFileSystemEntries(path)
                .Select(Path.GetFileName)
                .Any(name => !string.IsNullOrWhiteSpace(name) &&
                             !name.StartsWith(".", StringComparison.Ordinal) &&
                             !string.Equals(name, "Thumbs.db", StringComparison.OrdinalIgnoreCase));

            return hasVisibleEntries
                ? Ui("dialog.folder_explorer.status.files_no_project")
                : Ui("dialog.folder_explorer.status.empty");
        }
        catch
        {
            return Ui("dialog.folder_explorer.status.unavailable");
        }
    }
}

public sealed class DirectoryTreeNodeViewModel : ViewModelBase
{
    private readonly string _bytesLabel;
    private bool _childrenLoaded;
    private bool _isExpanded;

    public DirectoryTreeNodeViewModel(string name, string? fullPath, bool isVirtual = false, string bytesLabel = "Bytes")
    {
        Name = name;
        FullPath = fullPath;
        IsVirtual = isVirtual;
        _bytesLabel = bytesLabel;
        Children = [];
    }

    public string Name { get; }

    public string? FullPath { get; }

    public bool IsVirtual { get; }

    public ObservableCollection<DirectoryTreeNodeViewModel> Children { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public void PreparePlaceholder()
    {
        if (IsVirtual || string.IsNullOrWhiteSpace(FullPath))
        {
            return;
        }

        if (Children.Count == 0)
        {
            Children.Add(new DirectoryTreeNodeViewModel("...", null, isVirtual: true, bytesLabel: _bytesLabel));
        }
    }

    public void EnsureChildrenLoaded()
    {
        if (_childrenLoaded || IsVirtual || string.IsNullOrWhiteSpace(FullPath))
        {
            return;
        }

        _childrenLoaded = true;
        Children.Clear();

        try
        {
            foreach (var directory in Directory.EnumerateDirectories(FullPath)
                         .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
            {
                var childName = Path.GetFileName(directory);
                if (string.IsNullOrWhiteSpace(childName) || childName.StartsWith(".", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(childName))
                {
                    childName = directory;
                }

                var child = new DirectoryTreeNodeViewModel(childName, directory, bytesLabel: _bytesLabel);
                child.PreparePlaceholder();
                Children.Add(child);
            }
        }
        catch
        {
        }
    }

    public void ResetChildren()
    {
        if (IsVirtual)
        {
            return;
        }

        _childrenLoaded = false;
        Children.Clear();
        PreparePlaceholder();
    }

    public void CollectExpandedPaths(ISet<string> expandedPaths)
    {
        if (!string.IsNullOrWhiteSpace(FullPath) && IsExpanded)
        {
            expandedPaths.Add(FullPath);
        }

        foreach (var child in Children)
        {
            child.CollectExpandedPaths(expandedPaths);
        }
    }

    public void RestoreExpandedPaths(IReadOnlyCollection<string>? expandedPaths)
    {
        if (expandedPaths is null || expandedPaths.Count == 0)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(FullPath) &&
            expandedPaths.Contains(FullPath, StringComparer.OrdinalIgnoreCase))
        {
            EnsureChildrenLoaded();
            IsExpanded = true;
        }

        foreach (var child in Children)
        {
            child.RestoreExpandedPaths(expandedPaths);
        }
    }

    public DirectoryTreeNodeViewModel? FindOrExpandTo(string targetPath)
    {
        if (!string.IsNullOrWhiteSpace(FullPath) &&
            string.Equals(Path.GetFullPath(FullPath), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
        {
            return this;
        }

        if (!string.IsNullOrWhiteSpace(FullPath) &&
            targetPath.StartsWith(Path.GetFullPath(FullPath) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            EnsureChildrenLoaded();
            IsExpanded = true;

            foreach (var child in Children)
            {
                var match = child.FindOrExpandTo(targetPath);
                if (match is not null)
                {
                    return match;
                }
            }
        }

        foreach (var child in Children)
        {
            if (child.IsVirtual)
            {
                continue;
            }

            var match = child.FindOrExpandTo(targetPath);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}

public sealed class FileEntryViewModel
{
    public FileEntryViewModel(string name, string fullPath, string bytesLabel = "Bytes")
    {
        Name = name;
        FullPath = fullPath;

        try
        {
            var info = new FileInfo(fullPath);
            SizeText = info.Exists ? $"{info.Length:N0} {bytesLabel}" : string.Empty;
        }
        catch
        {
            SizeText = string.Empty;
        }
    }

    public string Name { get; }

    public string FullPath { get; }

    public string SizeText { get; }
}
