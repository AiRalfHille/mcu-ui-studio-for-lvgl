namespace Ai.McuUiStudio.Core.Model;

public sealed class UiNode
{
    public UiNode(string elementName)
    {
        ElementName = elementName;
    }

    public Guid Id { get; } = Guid.NewGuid();

    public string ElementName { get; }

    public Dictionary<string, string?> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<UiEventBinding> Events { get; } = [];

    public List<UiNode> Children { get; } = [];
}
