namespace Ai.McuUiStudio.Core.Services;

public sealed record LvglCGenerationResult(
    string HeaderFileName,
    string SourceFileName,
    string HeaderCode,
    string SourceCode);
