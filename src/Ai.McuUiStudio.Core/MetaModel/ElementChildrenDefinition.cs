namespace Ai.McuUiStudio.Core.MetaModel;

public sealed record ElementChildrenDefinition(
    IReadOnlyList<string> Allowed,
    int Min = 0,
    int? Max = null);
