using System.Text.Json;
using Ai.McuUiStudio.App.ViewModels;

namespace Ai.McuUiStudio.App.Services.Project;

public static class ProjectFileSerializer
{
    public static ProjectSettingsViewModel Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<ProjectFileDocument>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new ProjectFileDocument();

        var settings = new ProjectSettingsViewModel
        {
            FormatVersion = document.FormatVersion <= 0 ? 1 : document.FormatVersion,
            LvglVersion = string.IsNullOrWhiteSpace(document.LvglVersion) ? "9.4" : document.LvglVersion,
            Mode = string.IsNullOrWhiteSpace(document.Mode) ? "LVGL" : document.Mode,
            ProjectDirectory = string.IsNullOrWhiteSpace(document.ProjectDirectory) ? "." : document.ProjectDirectory,
            StrictValidation = document.StrictValidation,
            ScreenWidth = document.ScreenWidth <= 0 ? 1280 : document.ScreenWidth,
            ScreenHeight = document.ScreenHeight <= 0 ? 800 : document.ScreenHeight,
            ScreenFiles = JoinLines(document.ScreenFiles),
            AssetDirectories = JoinLines(document.AssetDirectories),
            FontDirectories = JoinLines(document.FontDirectories),
            OutputDirectory = string.IsNullOrWhiteSpace(document.OutputDirectory) ? "generated" : document.OutputDirectory,
            ProjectTemplate = string.IsNullOrWhiteSpace(document.ProjectTemplate) ? "Standard" : document.ProjectTemplate
        };

        if (!string.IsNullOrWhiteSpace(document.LvConfFile))
        {
            settings.LvConfFile = document.LvConfFile;
        }

        if (!string.IsNullOrWhiteSpace(document.ThemeFile))
        {
            settings.ThemeFile = document.ThemeFile;
        }

        return settings;
    }

    public static string Serialize(ProjectSettingsViewModel settings)
    {
        var document = new ProjectFileDocument
        {
            FormatVersion = settings.FormatVersion,
            LvglVersion = settings.LvglVersion,
            Mode = settings.Mode,
            ProjectDirectory = settings.ProjectDirectory,
            StrictValidation = settings.StrictValidation,
            ScreenWidth = settings.ScreenWidth,
            ScreenHeight = settings.ScreenHeight,
            ScreenFiles = SplitLines(settings.ScreenFiles),
            AssetDirectories = SplitLines(settings.AssetDirectories),
            FontDirectories = SplitLines(settings.FontDirectories),
            OutputDirectory = settings.OutputDirectory,
            LvConfFile = settings.LvConfFile,
            ThemeFile = settings.ThemeFile,
            ProjectTemplate = settings.ProjectTemplate
        };

        return JsonSerializer.Serialize(document, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string JoinLines(IEnumerable<string>? values)
    {
        return values is null
            ? string.Empty
            : string.Join(Environment.NewLine, values.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string[] SplitLines(string? value)
    {
        return (value ?? string.Empty)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private sealed class ProjectFileDocument
    {
        public int FormatVersion { get; init; } = 1;

        public string LvglVersion { get; init; } = "9.4";

        public string Mode { get; init; } = "LVGL";

        public string ProjectDirectory { get; init; } = ".";

        public bool StrictValidation { get; init; } = true;

        public int ScreenWidth { get; init; } = 1280;

        public int ScreenHeight { get; init; } = 800;

        public string[] ScreenFiles { get; init; } = [];

        public string[] AssetDirectories { get; init; } = [];

        public string[] FontDirectories { get; init; } = [];

        public string OutputDirectory { get; init; } = "generated";

        public string LvConfFile { get; init; } = "generated/lv_conf_project.h";

        public string ThemeFile { get; init; } = "theme_project.c";

        public string ProjectTemplate { get; init; } = "Standard";
    }
}
