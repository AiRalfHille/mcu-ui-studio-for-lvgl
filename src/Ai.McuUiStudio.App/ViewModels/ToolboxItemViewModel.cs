using Avalonia.Controls;
using Avalonia.Media;

namespace Ai.McuUiStudio.App.ViewModels;

public sealed class ToolboxItemViewModel
{
    public ToolboxItemViewModel(
        string label,
        string? toolName,
        bool isHeader,
        string? groupKey = null,
        bool isExpanded = true,
        bool isRuntimeSupported = true,
        string? availabilityHint = null)
    {
        Label = label;
        ToolName = toolName;
        IsHeader = isHeader;
        GroupKey = groupKey;
        IsExpanded = isExpanded;
        IsRuntimeSupported = isRuntimeSupported;
        AvailabilityHint = availabilityHint;
    }

    public string Label { get; }

    public string? ToolName { get; }

    public bool IsHeader { get; }

    public string? GroupKey { get; }

    public bool IsExpanded { get; }

    public bool IsRuntimeSupported { get; }

    public string? AvailabilityHint { get; }

    public object? AvailabilityTooltip =>
        string.IsNullOrWhiteSpace(AvailabilityHint)
            ? null
            : new TextBlock
            {
                MaxWidth = 360,
                Text = AvailabilityHint,
                TextWrapping = TextWrapping.Wrap
            };

    public bool ShowUnsupportedHint => !IsHeader && !IsRuntimeSupported;

    public double EntryOpacity => IsRuntimeSupported ? 1.0 : 0.45;

    public IBrush LabelBrush => IsRuntimeSupported
        ? Brushes.Black
        : new SolidColorBrush(Color.Parse("#A14F1A"));

    public bool IsSelectable => !IsHeader && IsRuntimeSupported && !string.IsNullOrWhiteSpace(ToolName);

    public bool ShowAsEntry => !IsHeader;

    public string ToggleGlyph => IsExpanded ? "▼" : "▶";
}
