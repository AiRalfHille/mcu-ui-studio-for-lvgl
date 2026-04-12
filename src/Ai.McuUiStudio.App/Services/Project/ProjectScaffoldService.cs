using System.Text;
using Ai.McuUiStudio.App.ViewModels;
using Ai.McuUiStudio.Core.Model;
using Ai.McuUiStudio.Core.Services;

namespace Ai.McuUiStudio.App.Services.Project;

public static class ProjectScaffoldService
{
    private static readonly JsonDocumentSerializer JsonDocumentSerializer = new();

    public static async Task EnsureProjectScaffoldAsync(ProjectSettingsViewModel settings)
    {
        var projectDirectory = settings.ProjectDirectory?.Trim();
        if (string.IsNullOrWhiteSpace(projectDirectory))
        {
            return;
        }

        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(settings.OutputDirectory);

        foreach (var relativeDirectory in EnumerateLines(settings.AssetDirectories))
        {
            Directory.CreateDirectory(ResolvePath(projectDirectory, relativeDirectory));
        }

        foreach (var relativeDirectory in EnumerateLines(settings.FontDirectories))
        {
            Directory.CreateDirectory(ResolvePath(projectDirectory, relativeDirectory));
        }

        foreach (var relativeFile in EnumerateLines(settings.ScreenFiles))
        {
            var screenPath = ResolvePath(projectDirectory, relativeFile);
            var screenDirectory = Path.GetDirectoryName(screenPath);
            if (!string.IsNullOrWhiteSpace(screenDirectory))
            {
                Directory.CreateDirectory(screenDirectory);
            }

            if (!File.Exists(screenPath))
            {
                await File.WriteAllTextAsync(screenPath, CreateDefaultScreenJson(settings), Encoding.UTF8);
            }
        }

        if (!File.Exists(settings.LvConfFile))
        {
            var templatePath = FindLvConfTemplatePath();
            if (!string.IsNullOrWhiteSpace(templatePath) && File.Exists(templatePath))
            {
                File.Copy(templatePath, settings.LvConfFile);
            }
        }

        if (!File.Exists(settings.ThemeFile))
        {
            await File.WriteAllTextAsync(settings.ThemeFile, CreateDefaultThemeSource(), Encoding.UTF8);
        }
    }

    private static IEnumerable<string> EnumerateLines(string rawValue)
    {
        return (rawValue ?? string.Empty)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }

    private static string ResolvePath(string projectDirectory, string path)
    {
        return Path.IsPathRooted(path) ? path : Path.Combine(projectDirectory, path);
    }

    private static string? FindLvConfTemplatePath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var overlayCandidate = Path.Combine(current.FullName, "templates", "lv_conf_project.h");
            if (File.Exists(overlayCandidate))
            {
                return overlayCandidate;
            }

            var candidate = Path.Combine(current.FullName, "native", "lvgl_simulator_host", "config", "lv_conf.h");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }

    public static string CreateDefaultScreenJson(ProjectSettingsViewModel settings, string screenName = "main_screen")
    {
        var screen = new UiNode("screen");
        screen.Attributes["name"] = string.IsNullOrWhiteSpace(screenName) ? "main_screen" : screenName;
        screen.Attributes["width"] = settings.ScreenWidth.ToString();
        screen.Attributes["height"] = settings.ScreenHeight.ToString();

        return JsonDocumentSerializer.Serialize(new UiDocument(screen));
    }

    private static string CreateDefaultThemeSource()
    {
        return """
#include "lvgl.h"

lv_theme_t * theme_project_create(lv_display_t * disp)
{
    return lv_theme_default_init(
        disp,
        lv_color_hex(0x2596be),
        lv_color_hex(0xff8a00),
        false,
        LV_FONT_DEFAULT);
}
""";
    }
}
