using Ai.McuUiStudio.Core.MetaModel;

namespace Ai.McuUiStudio.Core.Model;

public sealed record ElementAttributeDefinition(
    string Name,
    string TypeName,
    bool IsRequired = false,
    IReadOnlyList<string>? AllowedValues = null,
    AttributeTarget Target = AttributeTarget.Display);
