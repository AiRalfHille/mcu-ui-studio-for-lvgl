namespace Ai.McuUiStudio.Core.Services;

public sealed record McuDisplayCodeGenerationResult(
    string HeaderFileName,
    string SourceFileName,
    string HeaderCode,
    string SourceCode);
