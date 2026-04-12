using Ai.McuUiStudio.App.Services.Localization;
using Avalonia.Media;

namespace Ai.McuUiStudio.App.ViewModels;

public sealed class ProjectDialogViewModel : ViewModelBase
{
    private readonly LocalizationCatalog _localizationCatalog;
    private string? _projectFilePath;
    private string? _loadedProjectDirectory;

    public ProjectDialogViewModel(LocalizationCatalog localizationCatalog, ProjectSettingsViewModel settings, string? projectFilePath)
    {
        _localizationCatalog = localizationCatalog;
        Settings = settings;
        Settings.PropertyChanged += (_, e) =>
        {
            if (string.Equals(e.PropertyName, nameof(ProjectSettingsViewModel.ProjectDirectory), StringComparison.Ordinal))
            {
                RaisePropertyChanged(nameof(CanContinue));
                RaisePropertyChanged(nameof(OutputDirectoryBrush));
            }

            if (string.Equals(e.PropertyName, nameof(ProjectSettingsViewModel.OutputDirectory), StringComparison.Ordinal))
            {
                RaisePropertyChanged(nameof(OutputDirectoryBrush));
            }
        };
        ProjectFilePath = projectFilePath;
    }

    public ProjectSettingsViewModel Settings { get; }

    public string? ProjectFilePath
    {
        get => _projectFilePath;
        set
        {
            if (SetProperty(ref _projectFilePath, value))
            {
                _loadedProjectDirectory = string.IsNullOrWhiteSpace(value)
                    ? null
                    : NormalizeDirectoryPath(System.IO.Path.GetDirectoryName(value));
                RaisePropertyChanged(nameof(ProjectStatusText));
                RaisePropertyChanged(nameof(CanContinue));
            }
        }
    }

    public bool HasExistingProject => !string.IsNullOrWhiteSpace(ProjectFilePath);
    public bool CanContinue => HasExistingProject || !string.IsNullOrWhiteSpace(NormalizeDirectoryPath(Settings.ProjectDirectory));

    public string? LoadedProjectDirectory => _loadedProjectDirectory;

    public string ProjectStatusText => HasExistingProject
        ? Ui("dialog.project.status_existing")
        : Ui("dialog.project.status_new");

    private string Ui(string key) => _localizationCatalog.GetUiString(key);

    public string TitleText => Ui("dialog.project_title");
    public string GeneralSectionTitle => Ui("dialog.project.general");
    public string PathsSectionTitle => Ui("dialog.project.paths");
    public string ResourcesSectionTitle => Ui("dialog.project.resources");
    public string LoadLabel => Ui("dialog.load");
    public string ExistingProjectLabel => Ui("dialog.project_open_directory");
    public string ContinueLabel => Ui("dialog.next");
    public string CloseLabel => Ui("dialog.close");
    public string BackLabel => Ui("dialog.back");
    public string AddDirectoryLabel => Ui("dialog.add_directory");
    public string ProjectStatusLabel => Ui("dialog.project.status");
    public string FormatVersionLabel => Ui("dialog.project.format_version");
    public string LvglVersionLabel => Ui("dialog.project.lvgl_version");
    public string ModeLabel => Ui("dialog.project.mode");
    public string ProjectTemplateLabel => Ui("dialog.project.template_type");
    public string ProjectDirectoryLabel => Ui("dialog.project.project_directory");
    public string StrictValidationLabel => Ui("dialog.project.strict_validation");
    public string ScreenWidthLabel => Ui("dialog.project.screen_width");
    public string ScreenHeightLabel => Ui("dialog.project.screen_height");
    public string OutputDirectoryLabel => Ui("dialog.project.output_directory");
    public IBrush OutputDirectoryBrush => HasDefaultOutputDirectory
        ? Brushes.Black
        : new SolidColorBrush(Color.Parse("#A14F1A"));
    public string SelectOutputDirectoryLabel => Ui("dialog.project.select_output_directory");
    public string LvConfFileLabel => Ui("dialog.project.lvconf_file");
    public string ThemeFileLabel => Ui("dialog.project.theme_file");
    public string ScreenFilesLabel => Ui("dialog.project.screen_files");
    public string AssetDirectoriesLabel => Ui("dialog.project.asset_directories");
    public string FontDirectoriesLabel => Ui("dialog.project.font_directories");
    public IReadOnlyList<string> ProjectTemplateOptions { get; } = ["Standard", "RTOS-Messages"];
    public string MultilineHint => Ui("dialog.project.hint_multiline");
    public string OpenProjectDialogTitle => Ui("dialog.project_open_title");
    public string OpenProjectDirectoryDialogTitle => Ui("dialog.project_open_directory_title");
    public string SaveProjectDialogTitle => Ui("dialog.project_save_title");
    public string ProjectFileTypeLabel => Ui("dialog.filetype.project");
    public string ProjectLoadFailedFormat => Ui("error.project_load_failed");
    public string ProjectSaveFailedFormat => Ui("error.project_save_failed");
    public string ProjectDirectoryRequiredMessage => Ui("error.project_directory_required");
    public string ProjectDirectoryNoProjectFormat => Ui("error.project_directory_no_project");
    public string ProjectDirectoryMultipleProjectsFormat => Ui("error.project_directory_multiple_projects");
    public string ProjectDirectoryContainsProjectFormat => Ui("error.project_directory_contains_project");
    public string SelectLvConfFileDialogTitle => Ui("dialog.project_select_lvconf_title");
    public string SelectThemeFileDialogTitle => Ui("dialog.project_select_theme_title");
    public string SelectOutputDirectoryDialogTitle => Ui("dialog.project_select_output_directory_title");
    public string ConfirmTitle => Ui("dialog.confirm_title");
    public string ConfirmYesLabel => Ui("dialog.confirm_yes");
    public string ConfirmNoLabel => Ui("dialog.confirm_no");
    public string SwitchToNewProjectFormat => Ui("dialog.project_switch_to_new");
    public string SwitchToNewProjectLabel => Ui("dialog.project_switch_new_button");
    public string SwitchToCopyProjectLabel => Ui("dialog.project_switch_copy_button");
    public string SwitchProjectCancelLabel => Ui("dialog.project_switch_cancel_button");
    public string AdjustCopiedProjectPathsFormat => Ui("dialog.project_adjust_copied_paths");
    public string AdjustCopiedProjectPathsLabel => Ui("dialog.project_adjust_copied_paths_button");
    public string NewProjectDirectoryNotEmptyFormat => Ui("dialog.project_directory_not_empty");
    public string NewProjectWithoutProjectFileFormat => Ui("dialog.project_directory_without_project");
    public string CopyTargetNotEmptyFormat => Ui("error.project_copy_target_not_empty");

    private bool HasDefaultOutputDirectory
    {
        get
        {
            var projectDirectory = NormalizeDirectoryPath(Settings.ProjectDirectory);
            var outputDirectory = NormalizeDirectoryPath(Settings.OutputDirectory);
            if (string.IsNullOrWhiteSpace(projectDirectory) || string.IsNullOrWhiteSpace(outputDirectory))
            {
                return true;
            }

            var expectedOutputDirectory = NormalizeDirectoryPath(System.IO.Path.Combine(projectDirectory, "build"));
            return string.Equals(outputDirectory, expectedOutputDirectory, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void MarkAsNewProject(string projectDirectory)
    {
        ProjectFilePath = null;
        Settings.ProjectDirectory = NormalizeDirectoryPath(projectDirectory) ?? projectDirectory;
    }

    public LocalizationCatalog GetLocalizationCatalog() => _localizationCatalog;

    public static string? NormalizeDirectoryPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return System.IO.Path.TrimEndingDirectorySeparator(path.Trim());
    }
}
