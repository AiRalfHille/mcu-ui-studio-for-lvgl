namespace Ai.McuUiStudio.App.ViewModels;

public sealed class PropertyGridItemViewModel
{
    public PropertyGridItemViewModel(
        string label,
        bool isHeader,
        AttributeEditorViewModel? editor = null,
        string? groupKey = null,
        bool isExpanded = true)
    {
        Label = label;
        IsHeader = isHeader;
        Editor = editor;
        GroupKey = groupKey;
        IsExpanded = isExpanded;
    }

    public string Label { get; }

    public bool IsHeader { get; }

    public AttributeEditorViewModel? Editor { get; }

    public string? GroupKey { get; }

    public bool IsExpanded { get; }

    public bool ShowAsEntry => !IsHeader && Editor is not null;

    public string ToggleGlyph => IsExpanded ? "▼" : "▶";
}
