namespace Ai.McuUiStudio.App.Services;

public static class AppRuntimePaths
{
    public static string ResolveDocumentationRoot()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "DocumentationSite"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "DocumentationSite"))
        };

        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }

    public static string? TryResolveBundledNativeSimulator()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "simulator", "lvgl_simulator_host"),
            Path.Combine(AppContext.BaseDirectory, "simulator", "lvgl_simulator_host.exe"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "simulator", "lvgl_simulator_host")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "Resources", "simulator", "lvgl_simulator_host.exe")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "simulator", "lvgl_simulator_host")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "simulator", "lvgl_simulator_host.exe"))
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
