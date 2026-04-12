using Ai.McuUiStudio.Core.MetaModel;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

public sealed class ContextEngine
{
    private readonly MetaModelRegistry _registry;

    public ContextEngine(MetaModelRegistry registry)
    {
        _registry = registry;
    }

    public IReadOnlyList<ElementDefinition> GetAllowedChildren(UiNode node)
    {
        if (!_registry.TryGet(node.ElementName, out var parentDefinition) || parentDefinition is null)
        {
            return [];
        }

        return _registry.Elements
            .Where(x => parentDefinition.Children.Allowed.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
