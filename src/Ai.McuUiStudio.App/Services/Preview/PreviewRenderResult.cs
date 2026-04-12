namespace Ai.McuUiStudio.App.Services.Preview;

public sealed record PreviewRenderResult(
    bool Success,
    bool IsConnected,
    string StatusMessage);
