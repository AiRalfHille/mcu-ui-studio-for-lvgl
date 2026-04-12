namespace Ai.McuUiStudio.Core.MetaModel;

public sealed record LvglElementAttributeDefinition(
    string Name,
    string DisplayName,
    string LvglName,
    string TypeName,
    bool IsRequired = false,
    IReadOnlyList<string>? AllowedValues = null,
    AttributeTarget Target = AttributeTarget.Display,
    bool Supported = true);
