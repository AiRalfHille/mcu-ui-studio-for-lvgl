namespace Ai.McuUiStudio.App.Services.Preview;

public enum PreviewBackendKind
{
    ManagedPreviewHost,
    NativeLvglPreview
}

public static class PreviewBackendCatalog
{
    public const string ManagedPreviewHostLabel = "C# Preview Host";
    public const string NativeLvglPreviewLabel = "Native LVGL Preview (SDL)";

    public static IReadOnlyList<string> All { get; } =
    [
        ManagedPreviewHostLabel,
        NativeLvglPreviewLabel
    ];

    public static IPreviewService CreateService(string label)
    {
        return new ProcessPreviewService(ToKind(label));
    }

    public static PreviewBackendKind ToKind(string? label)
    {
        return string.Equals(label, NativeLvglPreviewLabel, StringComparison.Ordinal)
            ? PreviewBackendKind.NativeLvglPreview
            : PreviewBackendKind.ManagedPreviewHost;
    }
}
