using System.Collections.ObjectModel;
using System.IO;
using Ai.McuUiStudio.App.Services.Localization;

namespace Ai.McuUiStudio.App.ViewModels;

public sealed class FolderPickerDialogViewModel : ViewModelBase
{
    private readonly LocalizationCatalog _localizationCatalog;
    private string _currentPath;
    private string _pathInput;
    private string _errorText = string.Empty;
    private FolderEntryViewModel? _selectedEntry;

    public FolderPickerDialogViewModel(LocalizationCatalog localizationCatalog, string title, string? initialPath)
    {
        _localizationCatalog = localizationCatalog;
        Title = string.IsNullOrWhiteSpace(title) ? Ui("dialog.folder_explorer_title") : title;
        Entries = [];

        var startPath = NormalizeDirectory(initialPath)
            ?? NormalizeDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))
            ?? NormalizeDirectory(Directory.GetCurrentDirectory())
            ?? Path.GetPathRoot(Directory.GetCurrentDirectory())
            ?? "/";

        _currentPath = startPath;
        _pathInput = startPath;
        RefreshEntries();
    }

    public string Title { get; }

    private string Ui(string key) => _localizationCatalog.GetUiString(key);

    public ObservableCollection<FolderEntryViewModel> Entries { get; }

    public string CurrentPath
    {
        get => _currentPath;
        private set
        {
            if (!SetProperty(ref _currentPath, value))
            {
                return;
            }

            PathInput = value;
            RaisePropertyChanged(nameof(CanGoUp));
        }
    }

    public string PathInput
    {
        get => _pathInput;
        set => SetProperty(ref _pathInput, value);
    }

    public string ErrorText
    {
        get => _errorText;
        private set => SetProperty(ref _errorText, value);
    }

    public FolderEntryViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                RaisePropertyChanged(nameof(CanOpenSelected));
            }
        }
    }

    public bool CanGoUp
    {
        get
        {
            var parent = GetParentDirectory(CurrentPath);
            return !string.IsNullOrWhiteSpace(parent) &&
                   !string.Equals(parent, CurrentPath, StringComparison.OrdinalIgnoreCase);
        }
    }

    public bool CanOpenSelected => SelectedEntry is not null;

    public string SelectCurrentLabel => Ui("dialog.folder_explorer.select_current");

    public string CancelLabel => Ui("dialog.close");

    public string UpLabel => Ui("dialog.folder_picker.up");

    public string HomeLabel => Ui("dialog.folder_picker.home");

    public string OpenLabel => Ui("dialog.load");

    public string GoLabel => Ui("dialog.folder_picker.go");

    public bool NavigateToSelected()
    {
        return SelectedEntry is not null && NavigateTo(SelectedEntry.FullPath);
    }

    public bool NavigateUp()
    {
        var parent = GetParentDirectory(CurrentPath);
        return !string.IsNullOrWhiteSpace(parent) && NavigateTo(parent);
    }

    public bool NavigateHome()
    {
        var home = NormalizeDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        return !string.IsNullOrWhiteSpace(home) && NavigateTo(home);
    }

    public bool NavigateToInput()
    {
        return NavigateTo(PathInput);
    }

    private bool NavigateTo(string? path)
    {
        var normalized = NormalizeDirectory(path);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            ErrorText = Ui("error.folder_picker.invalid_path");
            return false;
        }

        if (!Directory.Exists(normalized))
        {
            ErrorText = string.Format(
                Ui("error.folder_picker.not_found"),
                normalized);
            return false;
        }

        CurrentPath = normalized;
        ErrorText = string.Empty;
        SelectedEntry = null;
        RefreshEntries();
        return true;
    }

    private void RefreshEntries()
    {
        Entries.Clear();

        try
        {
            foreach (var directory in Directory.EnumerateDirectories(CurrentPath)
                         .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase))
            {
                var name = Path.GetFileName(directory);
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = directory;
                }

                Entries.Add(new FolderEntryViewModel(name, directory));
            }

            ErrorText = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
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

    private static string? GetParentDirectory(string path)
    {
        try
        {
            return Directory.GetParent(path)?.FullName;
        }
        catch
        {
            return null;
        }
    }
}

public sealed class FolderEntryViewModel
{
    public FolderEntryViewModel(string name, string fullPath)
    {
        Name = name;
        FullPath = fullPath;
    }

    public string Name { get; }

    public string FullPath { get; }
}
