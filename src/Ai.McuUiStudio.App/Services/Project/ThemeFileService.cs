using System.Text.RegularExpressions;

namespace Ai.McuUiStudio.App.Services.Project;

public sealed class ThemeFileService
{
    private static readonly Regex ThemeInitRegex = new(
        @"lv_theme_default_init\s*\(\s*disp\s*,\s*lv_color_hex\((?<primary>0x[0-9a-fA-F]+)\)\s*,\s*lv_color_hex\((?<secondary>0x[0-9a-fA-F]+)\)\s*,\s*(?<dark>true|false|0|1)\s*,\s*(?<font>[A-Za-z0-9_]+)\s*\)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    public ThemeProjectDocument Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return CreateDefaultDocument(filePath);
        }

        var source = File.ReadAllText(filePath);
        var match = ThemeInitRegex.Match(source);
        if (!match.Success)
        {
            return CreateDefaultDocument(filePath, source);
        }

        return new ThemeProjectDocument(
            filePath,
            source,
            match.Groups["primary"].Value,
            match.Groups["secondary"].Value,
            NormalizeBool(match.Groups["dark"].Value),
            match.Groups["font"].Value);
    }

    public void Save(ThemeProjectDocument document, string primaryColor, string secondaryColor, string darkMode, string font)
    {
        var match = ThemeInitRegex.Match(document.SourceCode);
        var sourceCode = match.Success ? document.SourceCode : CreateDefaultSource();
        var replacement = $"""
lv_theme_default_init(
        disp,
        lv_color_hex({NormalizeHex(primaryColor)}),
        lv_color_hex({NormalizeHex(secondaryColor)}),
        {NormalizeBool(darkMode)},
        {NormalizeFont(font)})
""";
        var updated = ThemeInitRegex.Replace(sourceCode, replacement, 1);
        File.WriteAllText(document.FilePath, updated);
    }

    private static ThemeProjectDocument CreateDefaultDocument(string filePath, string? _ = null)
    {
        return new ThemeProjectDocument(
            filePath,
            CreateDefaultSource(),
            "0x2596be",
            "0xff8a00",
            "false",
            "LV_FONT_DEFAULT");
    }

    private static string CreateDefaultSource()
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

    private static string NormalizeBool(string value)
    {
        return string.Equals(value?.Trim(), "1", StringComparison.Ordinal) ||
               string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
            ? "true"
            : "false";
    }

    private static string NormalizeHex(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "0x2596be";
        }

        if (trimmed.StartsWith("#", StringComparison.Ordinal))
        {
            return "0x" + trimmed[1..];
        }

        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return "0x" + trimmed[2..];
        }

        return "0x" + trimmed;
    }

    private static string NormalizeFont(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "LV_FONT_DEFAULT" : trimmed;
    }
}

public sealed record ThemeProjectDocument(
    string FilePath,
    string SourceCode,
    string PrimaryColor,
    string SecondaryColor,
    string DarkMode,
    string FontName);
