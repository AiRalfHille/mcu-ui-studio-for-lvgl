namespace Ai.McuUiStudio.Core.Services;

public sealed record RtosMessagesCodeGenerationResult(
    string ContractHeaderFileName,
    string ContractHeaderCode,
    string EventSourceFileName,
    string EventSourceCode,
    string UpdateSourceFileName,
    string UpdateSourceCode,
    string BindingCallCode,
    IReadOnlyList<string> ExportedObjectIds);
