namespace Ai.McuUiStudio.Core.MetaModel;

public sealed record LvglEventDefinition(
    string Name,
    string DisplayName,
    IReadOnlyList<LvglElementAttributeDefinition> Attributes);
