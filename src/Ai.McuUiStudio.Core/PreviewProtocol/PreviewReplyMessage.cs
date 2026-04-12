namespace Ai.McuUiStudio.Core.PreviewProtocol;

public sealed record PreviewReplyMessage(
    bool Success,
    string StatusMessage);
