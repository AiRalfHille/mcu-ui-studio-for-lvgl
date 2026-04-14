using System.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Ai.McuUiStudio.App.Services.Project;
using Ai.McuUiStudio.App.ViewModels;

namespace Ai.McuUiStudio.App.Views;

public partial class ProjectDialog : Window
{
    private enum ProjectDirectorySwitchChoice
    {
        Cancel,
        NewProject,
        CopyExisting
    }

    public string? SavedProjectFilePath { get; private set; }
    public bool WasAccepted { get; private set; }

    public ProjectDialog()
    {
        InitializeComponent();
    }

    private void CloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        WasAccepted = false;
        Close(false);
    }

    private async void LoadProjectClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ProjectDialogViewModel vm)
        {
            return;
        }

        var directoryPath = await PickFolderPathAsync(vm.OpenProjectDirectoryDialogTitle, vm.Settings.ProjectDirectory);
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return;
        }

        var projectFiles = Directory.GetFiles(directoryPath, "*.lvglproj", SearchOption.TopDirectoryOnly);
        if (projectFiles.Length == 0)
        {
            var canUseAsNewProject = IsEffectivelyEmptyDirectory(directoryPath) ||
                                     await ShowConfirmAsync(string.Format(vm.NewProjectWithoutProjectFileFormat, directoryPath));
            if (!canUseAsNewProject)
            {
                return;
            }

            if (await TrySwitchProjectDirectoryAsync(vm, directoryPath))
            {
                vm.MarkAsNewProject(directoryPath);
            }

            return;
        }

        if (projectFiles.Length > 1)
        {
            await ShowErrorAsync(string.Format(vm.ProjectDirectoryMultipleProjectsFormat, directoryPath));
            return;
        }

        await LoadProjectFromPathAsync(vm, projectFiles[0]);
    }

    private async Task LoadProjectFromPathAsync(ProjectDialogViewModel vm, string projectFilePath)
    {
        try
        {
            var json = await File.ReadAllTextAsync(projectFilePath);
            var loadedSettings = ProjectFileSerializer.Deserialize(json);
            var projectDirectory = ProjectDialogViewModel.NormalizeDirectoryPath(Path.GetDirectoryName(projectFilePath))
                ?? loadedSettings.ProjectDirectory;
            var adjustedCopiedProjectPaths = false;

            if (NeedsCopiedProjectPathAdjustment(loadedSettings, projectDirectory))
            {
                var adjustPaths = await ShowAdjustCopiedProjectPathsChoiceAsync(vm, projectDirectory, loadedSettings.OutputDirectory);
                if (!adjustPaths)
                {
                    return;
                }

                ApplyProjectPathAdjustment(loadedSettings, projectDirectory);
                adjustedCopiedProjectPaths = true;
            }

            if (adjustedCopiedProjectPaths)
            {
                var correctedProjectFilePath = GetDefaultProjectFilePath(projectDirectory);
                var adjustedJson = ProjectFileSerializer.Serialize(loadedSettings);
                await File.WriteAllTextAsync(correctedProjectFilePath, adjustedJson);

                if (!string.Equals(
                        Path.GetFullPath(projectFilePath),
                        Path.GetFullPath(correctedProjectFilePath),
                        StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(projectFilePath))
                {
                    File.Delete(projectFilePath);
                }

                projectFilePath = correctedProjectFilePath;
            }

            vm.Settings.CopyFrom(loadedSettings);
            vm.ProjectFilePath = projectFilePath;
            vm.Settings.ProjectDirectory = projectDirectory;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(string.Format(vm.ProjectLoadFailedFormat, ex.Message));
        }
    }

    private async void SaveProjectClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ProjectDialogViewModel vm)
        {
            return;
        }

        if (!await EnsureProjectDirectoryStateAsync(vm))
        {
            return;
        }

        string? targetPath = vm.ProjectFilePath;

        if (!string.IsNullOrWhiteSpace(targetPath))
        {
            try
            {
                await ProjectScaffoldService.EnsureProjectScaffoldAsync(vm.Settings);
                var json = ProjectFileSerializer.Serialize(vm.Settings);
                await File.WriteAllTextAsync(targetPath, json);
                SavedProjectFilePath = targetPath;
                vm.Settings.ProjectDirectory = ProjectDialogViewModel.NormalizeDirectoryPath(Path.GetDirectoryName(targetPath))
                    ?? vm.Settings.ProjectDirectory;
                WasAccepted = true;
                Close(true);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(string.Format(vm.ProjectSaveFailedFormat, ex.Message));
            }

            return;
        }

        var projectDirectory = vm.Settings.ProjectDirectory?.Trim();
        if (!string.IsNullOrWhiteSpace(projectDirectory))
        {
            try
            {
                Directory.CreateDirectory(projectDirectory);
                var projectName = Path.GetFileName(Path.TrimEndingDirectorySeparator(projectDirectory));
                if (string.IsNullOrWhiteSpace(projectName))
                {
                    projectName = "display";
                }

                targetPath = Path.Combine(projectDirectory, $"{projectName}.lvglproj");
                await ProjectScaffoldService.EnsureProjectScaffoldAsync(vm.Settings);
                var json = ProjectFileSerializer.Serialize(vm.Settings);
                await File.WriteAllTextAsync(targetPath, json);
                vm.ProjectFilePath = targetPath;
                var savedProjectDirectory = ProjectDialogViewModel.NormalizeDirectoryPath(Path.GetDirectoryName(targetPath))
                    ?? projectDirectory;
                vm.Settings.ProjectDirectory = savedProjectDirectory;
                SavedProjectFilePath = targetPath;
                WasAccepted = true;
                Close(true);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(string.Format(vm.ProjectSaveFailedFormat, ex.Message));
            }

            return;
        }

        await ShowErrorAsync(vm.ProjectDirectoryRequiredMessage);
    }

    private async void SelectOutputDirectoryClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not ProjectDialogViewModel vm)
        {
            return;
        }

        var path = await PickFolderPathAsync(vm.SelectOutputDirectoryDialogTitle, vm.Settings.OutputDirectory);
        if (!string.IsNullOrWhiteSpace(path))
        {
            vm.Settings.OutputDirectory = path;
        }
    }

    private async Task<string?> PickFolderPathAsync(string? title = null, string? suggestedPath = null)
    {
        if (StorageProvider is null)
        {
            return null;
        }

        var startLocation = !string.IsNullOrWhiteSpace(suggestedPath)
            ? await StorageProvider.TryGetFolderFromPathAsync(suggestedPath)
            : null;

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            SuggestedStartLocation = startLocation
        });

        return folders.Count > 0 ? folders[0].Path.LocalPath : null;
    }

    private static bool IsEffectivelyEmptyDirectory(string path)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(path))
        {
            var name = Path.GetFileName(entry);
            if (string.IsNullOrWhiteSpace(name) ||
                name.StartsWith(".", StringComparison.Ordinal) ||
                string.Equals(name, "Thumbs.db", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private async Task ShowErrorAsync(string message)
    {
        if (DataContext is not ProjectDialogViewModel vm)
        {
            return;
        }

        var dialog = new InfoDialog
        {
            DataContext = new InfoDialogViewModel(vm.TitleText, message, vm.CloseLabel)
        };

        await dialog.ShowDialog(this);
    }

    private async Task<bool> EnsureProjectDirectoryStateAsync(ProjectDialogViewModel vm)
    {
        var currentDirectory = ProjectDialogViewModel.NormalizeDirectoryPath(vm.Settings.ProjectDirectory);
        var loadedDirectory = ProjectDialogViewModel.NormalizeDirectoryPath(vm.LoadedProjectDirectory);

        if (!vm.HasExistingProject ||
            string.IsNullOrWhiteSpace(currentDirectory) ||
            string.IsNullOrWhiteSpace(loadedDirectory) ||
            string.Equals(currentDirectory, loadedDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return await TrySwitchProjectDirectoryAsync(vm, currentDirectory);
    }

    private async Task<bool> TrySwitchProjectDirectoryAsync(ProjectDialogViewModel vm, string newDirectory)
    {
        var loadedDirectory = ProjectDialogViewModel.NormalizeDirectoryPath(vm.LoadedProjectDirectory);
        newDirectory = ProjectDialogViewModel.NormalizeDirectoryPath(newDirectory) ?? newDirectory;

        if (!vm.HasExistingProject ||
            string.IsNullOrWhiteSpace(loadedDirectory) ||
            string.Equals(newDirectory, loadedDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var choice = await ShowProjectDirectorySwitchChoiceAsync(vm, newDirectory);
        if (choice == ProjectDirectorySwitchChoice.Cancel)
        {
            vm.Settings.ProjectDirectory = loadedDirectory;
            return false;
        }

        if (choice == ProjectDirectorySwitchChoice.CopyExisting)
        {
            if (Directory.Exists(newDirectory) && !IsEffectivelyEmptyDirectory(newDirectory))
            {
                await ShowErrorAsync(string.Format(vm.CopyTargetNotEmptyFormat, newDirectory));
                vm.Settings.ProjectDirectory = loadedDirectory;
                return false;
            }

            try
            {
                CopyProjectDirectory(loadedDirectory, newDirectory);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync(string.Format(vm.ProjectSaveFailedFormat, ex.Message));
                vm.Settings.ProjectDirectory = loadedDirectory;
                return false;
            }
        }

        vm.MarkAsNewProject(newDirectory);
        return true;
    }

    private static void CopyProjectDirectory(string sourceDirectory, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativeDirectory = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(targetDirectory, relativeDirectory));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);
            if (string.Equals(Path.GetExtension(file), ".lvglproj", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(fileName) ||
                fileName.StartsWith(".", StringComparison.Ordinal) ||
                string.Equals(fileName, "Thumbs.db", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var relativeFile = Path.GetRelativePath(sourceDirectory, file);
            var targetFile = Path.Combine(targetDirectory, relativeFile);
            var targetFileDirectory = Path.GetDirectoryName(targetFile);
            if (!string.IsNullOrWhiteSpace(targetFileDirectory))
            {
                Directory.CreateDirectory(targetFileDirectory);
            }

            File.Copy(file, targetFile, overwrite: false);
        }
    }

    private async Task<bool> ShowConfirmAsync(string message)
    {
        if (DataContext is not ProjectDialogViewModel vm)
        {
            return false;
        }

        var dialog = new ConfirmDialog
        {
            DataContext = new ConfirmDialogViewModel(vm.ConfirmTitle, message, vm.ConfirmYesLabel, vm.ConfirmNoLabel)
        };

        return await dialog.ShowDialog<bool>(this);
    }

    private async Task<ProjectDirectorySwitchChoice> ShowProjectDirectorySwitchChoiceAsync(ProjectDialogViewModel vm, string newDirectory)
    {
        var dialog = new ChoiceDialog
        {
            DataContext = new ChoiceDialogViewModel(
                vm.ConfirmTitle,
                string.Format(vm.SwitchToNewProjectFormat, newDirectory),
                vm.SwitchToNewProjectLabel,
                vm.SwitchToCopyProjectLabel,
                vm.SwitchProjectCancelLabel)
        };

        var result = await dialog.ShowDialog<ChoiceDialogResult>(this);
        return result switch
        {
            ChoiceDialogResult.Primary => ProjectDirectorySwitchChoice.NewProject,
            ChoiceDialogResult.Secondary => ProjectDirectorySwitchChoice.CopyExisting,
            _ => ProjectDirectorySwitchChoice.Cancel
        };
    }

    private async Task<bool> ShowAdjustCopiedProjectPathsChoiceAsync(ProjectDialogViewModel vm, string projectDirectory, string outputDirectory)
    {
        var dialog = new ChoiceDialog
        {
            DataContext = new ChoiceDialogViewModel(
                vm.ConfirmTitle,
                string.Format(vm.AdjustCopiedProjectPathsFormat, projectDirectory, outputDirectory),
                vm.AdjustCopiedProjectPathsLabel,
                string.Empty,
                vm.SwitchProjectCancelLabel)
        };

        var result = await dialog.ShowDialog<ChoiceDialogResult>(this);
        return result == ChoiceDialogResult.Primary;
    }

    private static bool NeedsCopiedProjectPathAdjustment(ProjectSettingsViewModel settings, string projectDirectory)
    {
        var normalizedProjectDirectory = ProjectDialogViewModel.NormalizeDirectoryPath(projectDirectory);
        if (string.IsNullOrWhiteSpace(normalizedProjectDirectory))
        {
            return false;
        }

        return IsPathOutsideProjectDirectory(settings.ProjectDirectory, normalizedProjectDirectory);
    }

    private static bool IsPathOutsideProjectDirectory(string? candidatePath, string projectDirectory)
    {
        var normalizedCandidate = ProjectDialogViewModel.NormalizeDirectoryPath(candidatePath);
        if (string.IsNullOrWhiteSpace(normalizedCandidate) || !Path.IsPathRooted(normalizedCandidate))
        {
            return false;
        }

        var fullProjectDirectory = Path.GetFullPath(projectDirectory);
        var fullCandidatePath = Path.GetFullPath(normalizedCandidate);
        return !fullCandidatePath.StartsWith(fullProjectDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(fullCandidatePath, fullProjectDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyProjectPathAdjustment(ProjectSettingsViewModel settings, string projectDirectory)
    {
        var adjustThemeFile = IsPathOutsideProjectDirectory(settings.ThemeFile, projectDirectory);
        var adjustLvConfFile = IsPathOutsideProjectDirectory(settings.LvConfFile, projectDirectory);

        settings.ProjectDirectory = projectDirectory;
        settings.OutputDirectory = Path.Combine(projectDirectory, "build");

        if (adjustThemeFile)
        {
            settings.ThemeFile = Path.Combine(projectDirectory, "theme_project.c");
        }

        if (adjustLvConfFile)
        {
            settings.LvConfFile = Path.Combine(projectDirectory, "build", "lv_conf_project.h");
        }
    }

    private static string GetDefaultProjectFilePath(string projectDirectory)
    {
        var projectName = Path.GetFileName(Path.TrimEndingDirectorySeparator(projectDirectory));
        if (string.IsNullOrWhiteSpace(projectName))
        {
            projectName = "display";
        }

        return Path.Combine(projectDirectory, $"{projectName}.lvglproj");
    }
}
