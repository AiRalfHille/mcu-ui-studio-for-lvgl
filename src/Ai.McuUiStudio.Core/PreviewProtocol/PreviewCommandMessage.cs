namespace Ai.McuUiStudio.Core.PreviewProtocol;

public sealed record PreviewCommandMessage(
    string Command,
    string DocumentName,
    string Content,
    bool ForceFullReload,
    int? ScreenWidth,
    int? ScreenHeight,
    int? ZoomPercent,
    bool ResetWindowToTargetSize);
