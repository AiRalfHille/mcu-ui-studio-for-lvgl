namespace Ai.McuUiStudio.Core.MetaModel;

public sealed record LvglMetaModelDefinition(
    string Name,
    string Version,
    IReadOnlyList<string> Targets,
    IReadOnlyList<AttributeValueTypeDefinition> AttributeTypes,
    IReadOnlyList<LvglElementDefinition> Elements);
