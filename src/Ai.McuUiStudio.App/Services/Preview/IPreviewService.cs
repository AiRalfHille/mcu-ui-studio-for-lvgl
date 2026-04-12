namespace Ai.McuUiStudio.App.Services.Preview;

public interface IPreviewService
{
    event EventHandler<string>? LogReceived;

    bool IsConnected { get; }

    string BackendName { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);

    Task<PreviewRenderResult> RenderAsync(
        PreviewRenderRequest request,
        CancellationToken cancellationToken = default);

    Task HighlightAsync(string? objectId, CancellationToken cancellationToken = default);
}
