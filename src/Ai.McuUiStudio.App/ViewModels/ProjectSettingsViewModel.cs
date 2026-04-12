using System.IO;

namespace Ai.McuUiStudio.App.ViewModels;

public sealed class ProjectSettingsViewModel : ViewModelBase
{
    private const string DefaultBuildDirectoryName = "build";
    private const string DefaultScreenFile = "screens/ui_start.json";
    private const string DefaultAssetsDirectory = "assets";
    private const string DefaultFontsDirectory = "fonts";
    private const string DefaultLvConfFileName = "lv_conf_project.h";
    private const string DefaultThemeFileName = "theme_project.c";
    private const int DefaultScreenWidthValue = 1280;
    private const int DefaultScreenHeightValue = 800;
    private const string DefaultProjectTemplate = "Standard";
    private int _formatVersion = 1;
    private string _lvglVersion = "9.4";
    private string _mode = "LVGL";
    private string _projectDirectory = string.Empty;
    private bool _strictValidation = true;
    private int _screenWidth = DefaultScreenWidthValue;
    private int _screenHeight = DefaultScreenHeightValue;
    private string _screenFiles = DefaultScreenFile;
    private string _assetDirectories = DefaultAssetsDirectory;
    private string _fontDirectories = DefaultFontsDirectory;
    private string _outputDirectory = string.Empty;
    private string _lvConfFile = string.Empty;
    private string _themeFile = string.Empty;
    private string _projectTemplate = DefaultProjectTemplate;
    private bool _usesDefaultOutputDirectory = true;
    private bool _usesDefaultThemeFile = true;

    public ProjectSettingsViewModel()
    {
        RefreshProjectLayout();
    }

    public int FormatVersion
    {
        get => _formatVersion;
        set => SetProperty(ref _formatVersion, value);
    }

    public string LvglVersion
    {
        get => _lvglVersion;
        set => SetProperty(ref _lvglVersion, value);
    }

    public string Mode
    {
        get => _mode;
        set => SetProperty(ref _mode, value);
    }

    public string ProjectDirectory
    {
        get => _projectDirectory;
        set
        {
            if (SetProperty(ref _projectDirectory, value))
            {
                RefreshProjectLayout();
            }
        }
    }

    public bool StrictValidation
    {
        get => _strictValidation;
        set => SetProperty(ref _strictValidation, value);
    }

    public int ScreenWidth
    {
        get => _screenWidth;
        set => SetProperty(ref _screenWidth, value <= 0 ? DefaultScreenWidthValue : value);
    }

    public int ScreenHeight
    {
        get => _screenHeight;
        set => SetProperty(ref _screenHeight, value <= 0 ? DefaultScreenHeightValue : value);
    }

    public string ScreenFiles
    {
        get => _screenFiles;
        set => SetProperty(ref _screenFiles, NormalizeNonEmpty(value, DefaultScreenFile));
    }

    public string AssetDirectories
    {
        get => _assetDirectories;
        set => SetProperty(ref _assetDirectories, NormalizeNonEmpty(value, DefaultAssetsDirectory));
    }

    public string FontDirectories
    {
        get => _fontDirectories;
        set => SetProperty(ref _fontDirectories, NormalizeNonEmpty(value, DefaultFontsDirectory));
    }

    public string OutputDirectory
    {
        get => _outputDirectory;
        set => SetOutputDirectory(value, useDefaultLayout: false);
    }

    public string LvConfFile
    {
        get => _lvConfFile;
        set => SetProperty(ref _lvConfFile, value);
    }

    public string ThemeFile
    {
        get => _themeFile;
        set => SetThemeFile(value, useDefaultLayout: false);
    }

    public string ProjectTemplate
    {
        get => _projectTemplate;
        set => SetProperty(ref _projectTemplate, NormalizeNonEmpty(value, DefaultProjectTemplate));
    }

    public ProjectSettingsViewModel Clone()
    {
        var clone = new ProjectSettingsViewModel
        {
            FormatVersion = FormatVersion,
            LvglVersion = LvglVersion,
            Mode = Mode,
            ProjectDirectory = ProjectDirectory,
            StrictValidation = StrictValidation,
            ScreenWidth = ScreenWidth,
            ScreenHeight = ScreenHeight,
            ScreenFiles = ScreenFiles,
            AssetDirectories = AssetDirectories,
            FontDirectories = FontDirectories,
            OutputDirectory = OutputDirectory,
            LvConfFile = LvConfFile,
            ThemeFile = ThemeFile,
            ProjectTemplate = ProjectTemplate
        };
        clone._usesDefaultOutputDirectory = _usesDefaultOutputDirectory;
        clone._usesDefaultThemeFile = _usesDefaultThemeFile;
        if (!_usesDefaultOutputDirectory)
        {
            clone.SetOutputDirectory(OutputDirectory, useDefaultLayout: false);
        }

        if (!_usesDefaultThemeFile)
        {
            clone.SetThemeFile(ThemeFile, useDefaultLayout: false);
        }

        return clone;
    }

    public void CopyFrom(ProjectSettingsViewModel other)
    {
        FormatVersion = other.FormatVersion;
        LvglVersion = other.LvglVersion;
        Mode = other.Mode;
        ProjectDirectory = other.ProjectDirectory;
        StrictValidation = other.StrictValidation;
        ScreenWidth = other.ScreenWidth;
        ScreenHeight = other.ScreenHeight;
        ScreenFiles = other.ScreenFiles;
        AssetDirectories = other.AssetDirectories;
        FontDirectories = other.FontDirectories;
        ProjectTemplate = other.ProjectTemplate;
        _usesDefaultOutputDirectory = other._usesDefaultOutputDirectory;
        _usesDefaultThemeFile = other._usesDefaultThemeFile;
        RefreshProjectLayout();
        if (!_usesDefaultOutputDirectory)
        {
            SetOutputDirectory(other.OutputDirectory, useDefaultLayout: false);
        }

        if (!_usesDefaultThemeFile)
        {
            SetThemeFile(other.ThemeFile, useDefaultLayout: false);
        }
    }

    private string CombineProjectDirectory(string childName)
    {
        var directory = ProjectDirectory?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(directory)
            ? childName
            : Path.Combine(directory, childName);
    }

    private void RefreshProjectLayout()
    {
        if (string.IsNullOrWhiteSpace(ProjectDirectory))
        {
            if (_usesDefaultOutputDirectory)
            {
                SetOutputDirectory(string.Empty, useDefaultLayout: true);
            }
            else
            {
                LvConfFile = string.Empty;
            }

            if (_usesDefaultThemeFile)
            {
                SetThemeFile(string.Empty, useDefaultLayout: true);
            }
            else
            {
                ThemeFile = string.Empty;
            }

            ScreenFiles = DefaultScreenFile;
            AssetDirectories = DefaultAssetsDirectory;
            FontDirectories = DefaultFontsDirectory;
            return;
        }

        if (_usesDefaultOutputDirectory || string.IsNullOrWhiteSpace(_outputDirectory))
        {
            SetOutputDirectory(CombineProjectDirectory(DefaultBuildDirectoryName), useDefaultLayout: true);
        }
        else
        {
            LvConfFile = Path.Combine(_outputDirectory, DefaultLvConfFileName);
        }

        if (_usesDefaultThemeFile || string.IsNullOrWhiteSpace(_themeFile))
        {
            SetThemeFile(CombineProjectDirectory(DefaultThemeFileName), useDefaultLayout: true);
        }

        ScreenFiles = DefaultScreenFile;
        AssetDirectories = DefaultAssetsDirectory;
        FontDirectories = DefaultFontsDirectory;
    }

    private void SetOutputDirectory(string? value, bool useDefaultLayout)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? (string.IsNullOrWhiteSpace(ProjectDirectory) ? string.Empty : CombineProjectDirectory(DefaultBuildDirectoryName))
            : value.Trim();

        _usesDefaultOutputDirectory = useDefaultLayout;
        if (SetProperty(ref _outputDirectory, normalized, nameof(OutputDirectory)))
        {
            LvConfFile = string.IsNullOrWhiteSpace(normalized) ? string.Empty : Path.Combine(normalized, DefaultLvConfFileName);
        }
        else
        {
            LvConfFile = string.IsNullOrWhiteSpace(normalized) ? string.Empty : Path.Combine(normalized, DefaultLvConfFileName);
        }
    }

    private void SetThemeFile(string? value, bool useDefaultLayout)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? (string.IsNullOrWhiteSpace(ProjectDirectory) ? string.Empty : CombineProjectDirectory(DefaultThemeFileName))
            : value.Trim();

        _usesDefaultThemeFile = useDefaultLayout;
        SetProperty(ref _themeFile, normalized, nameof(ThemeFile));
    }

    private static string NormalizeNonEmpty(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
