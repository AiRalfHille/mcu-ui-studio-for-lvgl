namespace Ai.McuUiStudio.App.Services.Preview;

public sealed class StubPreviewService : IPreviewService
{
    public event EventHandler<string>? LogReceived;

    public bool IsConnected { get; private set; }

    public string BackendName => "Stub Preview";

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = true;
        LogReceived?.Invoke(this, "[stub] Preview-Service verbunden.");
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = false;
        LogReceived?.Invoke(this, "[stub] Preview-Service getrennt.");
        return Task.CompletedTask;
    }

    public Task HighlightAsync(string? objectId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<PreviewRenderResult> RenderAsync(
        PreviewRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            await ConnectAsync(cancellationToken);
        }

        await Task.Delay(20, cancellationToken);
        LogReceived?.Invoke(this, $"[stub] Render fuer '{request.DocumentName}' empfangen.");

        return new PreviewRenderResult(
            true,
            IsConnected,
            $"Preview an {BackendName} gesendet ({request.DocumentName}).");
    }
}
