using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.MetaModel;

public sealed record AttributeValueTypeDefinition(
    string Name,
    AttributeType Kind,
    IReadOnlyList<string> AllowedValues);
