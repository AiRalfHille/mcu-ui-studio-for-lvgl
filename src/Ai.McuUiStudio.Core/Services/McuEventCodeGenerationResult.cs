namespace Ai.McuUiStudio.Core.Services;

public sealed record McuEventCodeGenerationResult(
    string HeaderFileName,
    string SourceFileName,
    string HeaderCode,
    string SourceCode,
    string BindingCallCode,
    bool HasBindings);
