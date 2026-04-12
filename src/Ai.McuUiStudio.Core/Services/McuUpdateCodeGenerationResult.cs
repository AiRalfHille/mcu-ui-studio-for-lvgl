namespace Ai.McuUiStudio.Core.Services;

public sealed record McuUpdateCodeGenerationResult(
    string HeaderFileName,
    string SourceFileName,
    string HeaderCode,
    string SourceCode,
    bool HasTargets);
