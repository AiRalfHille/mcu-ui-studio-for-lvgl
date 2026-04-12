using Ai.McuUiStudio.Core.PreviewProtocol;

namespace Ai.McuUiStudio.App.Services.Preview;

// Platzhalter fuer die spaetere externe C-Simulator-Anbindung.
public static class SimulatorProtocolNotes
{
    public const string ProtocolName = PreviewProtocolConstants.ProtocolName;
    public const int ProtocolVersion = PreviewProtocolConstants.ProtocolVersion;
    public const string RenderCommand = PreviewProtocolConstants.RenderCommand;
    public const string ReloadCommand = PreviewProtocolConstants.ReloadCommand;
    public const string ShutdownCommand = PreviewProtocolConstants.ShutdownCommand;
}
