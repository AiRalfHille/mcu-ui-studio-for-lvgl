namespace Ai.McuUiStudio.Core.MetaModel;

public sealed record LvglElementDefinition(
    string Name,
    string DisplayName,
    string Category,
    string Kind,
    IReadOnlyDictionary<string, LvglElementTargetDefinition> Targets,
    IReadOnlyList<LvglElementAttributeDefinition> Attributes,
    IReadOnlyList<LvglEventDefinition> Events,
    ElementChildrenDefinition Children,
    bool Supported = true);
