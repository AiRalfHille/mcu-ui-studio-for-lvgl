using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.App.ViewModels;

public sealed class NodeViewModel : ViewModelBase
{
    private bool _isExpanded = true;

    public NodeViewModel(
        UiNode node,
        string? displayName = null,
        bool isRuntimeSupported = true,
        string? availabilityHint = null)
    {
        Node = node;
        DisplayName = displayName;
        IsRuntimeSupported = isRuntimeSupported;
        AvailabilityHint = availabilityHint;
        Children = new ObservableCollection<NodeViewModel>(node.Children.Select(x => new NodeViewModel(x)));
    }

    public UiNode Node { get; }

    public string? DisplayName { get; }

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

    public bool ShowUnsupportedHint => !IsRuntimeSupported;

    public double EntryOpacity => IsRuntimeSupported ? 1.0 : 0.45;

    public ObservableCollection<NodeViewModel> Children { get; }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public string Header =>
        Node.Attributes.TryGetValue("id", out var id) && !string.IsNullOrWhiteSpace(id)
            ? $"{(DisplayName ?? Node.ElementName)} ({id})"
            : (DisplayName ?? Node.ElementName);

    public void Refresh()
    {
        RaisePropertyChanged(nameof(Header));
    }

    public void SetExpandedRecursive(bool isExpanded)
    {
        IsExpanded = isExpanded;

        foreach (var child in Children)
        {
            child.SetExpandedRecursive(isExpanded);
        }
    }
}
