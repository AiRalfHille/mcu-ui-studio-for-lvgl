namespace Ai.McuUiStudio.App.Services.Preview;

public sealed record PreviewRenderRequest(
    string Content,
    string DocumentName,
    bool ForceFullReload,
    int? ScreenWidth,
    int? ScreenHeight,
    int? ZoomPercent,
    bool ResetWindowToTargetSize);
