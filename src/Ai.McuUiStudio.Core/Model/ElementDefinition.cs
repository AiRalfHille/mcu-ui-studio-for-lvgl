using Ai.McuUiStudio.Core.MetaModel;

namespace Ai.McuUiStudio.Core.Model;

public sealed record ElementDefinition(
    string Name,
    string DisplayName,
    string Category,
    IReadOnlyList<ElementAttributeDefinition> Attributes,
    ElementChildrenDefinition Children);
