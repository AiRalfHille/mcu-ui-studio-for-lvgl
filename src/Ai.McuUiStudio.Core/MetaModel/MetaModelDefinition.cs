using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.MetaModel;

public sealed record MetaModelDefinition(
    string Name,
    string Version,
    IReadOnlyList<AttributeValueTypeDefinition> AttributeTypes,
    IReadOnlyList<ElementDefinition> Elements);
