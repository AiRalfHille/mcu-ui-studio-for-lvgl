namespace Ai.McuUiStudio.Core.Model;

public sealed class UiEventBinding
{
    public UiEventBinding(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public Dictionary<string, string?> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
}
