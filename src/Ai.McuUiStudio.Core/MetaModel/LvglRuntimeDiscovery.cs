using System.Text.RegularExpressions;

namespace Ai.McuUiStudio.Core.MetaModel;

public static class LvglRuntimeDiscovery
{
    private static readonly Regex MajorRegex = new(@"#define\s+LVGL_VERSION_MAJOR\s+(?<value>\d+)", RegexOptions.Compiled);
    private static readonly Regex MinorRegex = new(@"#define\s+LVGL_VERSION_MINOR\s+(?<value>\d+)", RegexOptions.Compiled);
    private static readonly Regex PatchRegex = new(@"#define\s+LVGL_VERSION_PATCH\s+(?<value>\d+)", RegexOptions.Compiled);

    public static IReadOnlyList<InstalledLvglRuntime> DiscoverFromDirectory(string thirdPartyDirectory)
    {
        if (string.IsNullOrWhiteSpace(thirdPartyDirectory) || !Directory.Exists(thirdPartyDirectory))
        {
            return [];
        }

        var candidates = Directory.EnumerateDirectories(thirdPartyDirectory, "lvgl*", SearchOption.TopDirectoryOnly)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase);

        var results = new List<InstalledLvglRuntime>();
        foreach (var candidate in candidates)
        {
            var versionHeaderPath = Path.Combine(candidate, "lv_version.h");
            if (!File.Exists(versionHeaderPath))
            {
                continue;
            }

            var version = TryReadVersion(versionHeaderPath);
            if (version is null)
            {
                continue;
            }

            results.Add(new InstalledLvglRuntime(version, candidate, versionHeaderPath));
        }

        return results;
    }

    private static string? TryReadVersion(string versionHeaderPath)
    {
        var content = File.ReadAllText(versionHeaderPath);
        var major = TryReadPart(MajorRegex, content);
        var minor = TryReadPart(MinorRegex, content);
        var patch = TryReadPart(PatchRegex, content);

        return major is null || minor is null || patch is null
            ? null
            : $"{major}.{minor}.{patch}";
    }

    private static string? TryReadPart(Regex regex, string content)
    {
        var match = regex.Match(content);
        return match.Success ? match.Groups["value"].Value : null;
    }
}
