using System.Collections.ObjectModel;
using System.IO;
using Ai.McuUiStudio.App.Services.Localization;

namespace Ai.McuUiStudio.App.ViewModels;

public sealed class ScreenFileDialogViewModel : ViewModelBase
{
    private readonly LocalizationCatalog _localizationCatalog;
    private readonly string _screensRootDirectory;
    private readonly bool _createMode;
    private DirectoryTreeNodeViewModel? _selectedDirectory;
    private FileEntryViewModel? _selectedFile;
    private string _currentDirectoryPath = string.Empty;
    private string _errorText = string.Empty;
    private string _newFileName = string.Empty;

    public ScreenFileDialogViewModel(LocalizationCatalog localizationCatalog, string title, string screensRootDirectory, bool createMode)
    {
        _localizationCatalog = localizationCatalog;
        Title = title;
        _screensRootDirectory = Path.GetFullPath(screensRootDirectory);
        _createMode = createMode;
        RootNodes = [];
        FileEntries = [];

        var rootNode = new DirectoryTreeNodeViewModel(Path.GetFileName(_screensRootDirectory), _screensRootDirectory, bytesLabel: Ui("dialog.file.bytes"));
        rootNode.PreparePlaceholder();
        rootNode.IsExpanded = true;
        RootNodes.Add(rootNode);
        SelectedDirectory = rootNode;
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

            RaisePropertyChanged(nameof(CanCreateFile));
            RaisePropertyChanged(nameof(CanCreateFromInput));
        }
    }

    public FileEntryViewModel? SelectedFile
    {
        get => _selectedFile;
        set
        {
            if (SetProperty(ref _selectedFile, value))
            {
                RaisePropertyChanged(nameof(CanOpenFile));
            }
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

    public string ConfirmLabel => _createMode
        ? Ui("dialog.screen_file.create")
        : Ui("dialog.load");

    public string CancelLabel => Ui("dialog.close");

    public string NewFilePlaceholder => Ui("dialog.screen_file.name_placeholder");

    public string ErrorDialogTitle => _createMode
        ? Ui("dialog.new_screen_title")
        : Ui("dialog.open_screen_title");

    public string NewFileName
    {
        get => _newFileName;
        set
        {
            if (SetProperty(ref _newFileName, value))
            {
                RaisePropertyChanged(nameof(CanCreateFromInput));
            }
        }
    }

    public bool IsCreateMode => _createMode;

    public bool IsOpenMode => !_createMode;

    public bool CanOpenFile => SelectedFile is not null;

    public bool CanCreateFile => SelectedDirectory is { IsVirtual: false, FullPath: not null };

    public bool CanCreateFromInput => IsCreateMode && CanCreateFile && !string.IsNullOrWhiteSpace(NewFileName);

    public void ExpandNode(DirectoryTreeNodeViewModel? node)
    {
        node?.EnsureChildrenLoaded();
    }

    public string? GetSelectedFilePath() => SelectedFile?.FullPath;

    public bool TryCreateFilePath(string fileName, out string? targetPath, out string? errorMessage)
    {
        targetPath = null;
        errorMessage = null;

        if (SelectedDirectory?.FullPath is not { } selectedDirectory)
        {
            errorMessage = Ui("error.screen_file.select_target");
            return false;
        }

        var trimmedName = fileName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            errorMessage = Ui("error.screen_file.enter_name");
            return false;
        }

        if (trimmedName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            errorMessage = Ui("error.screen_file.invalid_name");
            return false;
        }

        if (!trimmedName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            trimmedName += ".json";
        }

        targetPath = Path.Combine(selectedDirectory, trimmedName);
        if (File.Exists(targetPath))
        {
            errorMessage = string.Format(
                Ui("error.screen_file.exists"),
                trimmedName);
            return false;
        }

        return true;
    }

    public bool TryCreateFilePathFromInput(out string? targetPath, out string? errorMessage)
    {
        return TryCreateFilePath(NewFileName, out targetPath, out errorMessage);
    }

    public void RestoreState(IReadOnlyCollection<string>? expandedPaths, string? selectedDirectoryPath)
    {
        foreach (var rootNode in RootNodes)
        {
            rootNode.RestoreExpandedPaths(expandedPaths);
        }

        var targetPath = string.IsNullOrWhiteSpace(selectedDirectoryPath)
            ? _screensRootDirectory
            : Path.GetFullPath(selectedDirectoryPath);

        foreach (var rootNode in RootNodes)
        {
            var match = rootNode.FindOrExpandTo(targetPath);
            if (match is not null)
            {
                SelectedDirectory = match;
                return;
            }
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

    private void RefreshFiles(string? directoryPath)
    {
        FileEntries.Clear();
        SelectedFile = null;

        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            ErrorText = string.Empty;
            return;
        }

        try
        {
            foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
            {
                var fileName = Path.GetFileName(filePath);
                if (string.IsNullOrWhiteSpace(fileName) || fileName.StartsWith(".", StringComparison.Ordinal))
                {
                    continue;
                }

                FileEntries.Add(new FileEntryViewModel(fileName, filePath, Ui("dialog.file.bytes")));
            }

            ErrorText = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
    }
}
