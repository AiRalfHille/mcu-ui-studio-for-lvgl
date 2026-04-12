using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Ai.McuUiStudio.Core.PreviewProtocol;
using Ai.McuUiStudio.PreviewHost.ViewModels;

namespace Ai.McuUiStudio.PreviewHost.Services;

public sealed class PreviewTcpServer
{
    private readonly IClassicDesktopStyleApplicationLifetime _desktop;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private readonly int _port;
    private readonly PreviewHostWindowViewModel _viewModel;

    public PreviewTcpServer(
        PreviewHostWindowViewModel viewModel,
        IClassicDesktopStyleApplicationLifetime desktop,
        int port)
    {
        _viewModel = viewModel;
        _desktop = desktop;
        _port = port;
    }

    public async Task StartAsync()
    {
        var listener = new TcpListener(IPAddress.Loopback, _port);
        listener.Start();
        Console.WriteLine($"Preview host listening on port {_port}.");

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _viewModel.Status = $"Preview-Host wartet auf Verbindung auf Port {_port}.";
        });

        while (true)
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true)
            {
                AutoFlush = true
            };

            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line is null)
                {
                    break;
                }

                var message = JsonSerializer.Deserialize<PreviewCommandMessage>(line, _jsonOptions);
                if (message is null)
                {
                    Console.Error.WriteLine("Received invalid preview command.");
                    var invalidReply = new PreviewReplyMessage(false, "Ungueltige Preview-Nachricht.");
                    await writer.WriteLineAsync(JsonSerializer.Serialize(invalidReply, _jsonOptions));
                    continue;
                }

                if (string.Equals(
                        message.Command,
                        PreviewProtocolConstants.ShutdownCommand,
                        StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Preview host shutdown requested.");
                    var reply = new PreviewReplyMessage(true, "Preview-Host wird beendet.");
                    await writer.WriteLineAsync(JsonSerializer.Serialize(reply, _jsonOptions));
                    await Dispatcher.UIThread.InvokeAsync(() => _desktop.Shutdown());
                    return;
                }

                Console.WriteLine(
                    $"Preview command '{message.Command}' for '{message.DocumentName}' with {message.Content.Length} code chars. size={message.ScreenWidth?.ToString() ?? "auto"}x{message.ScreenHeight?.ToString() ?? "auto"} zoom={message.ZoomPercent?.ToString() ?? "auto"}%");

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _viewModel.DocumentName = message.DocumentName;
                    _viewModel.Content = message.Content;
                    _viewModel.LastUpdatedAt = DateTimeOffset.Now;
                    _viewModel.Status = string.Equals(
                            message.Command,
                            PreviewProtocolConstants.ReloadCommand,
                            StringComparison.OrdinalIgnoreCase)
                        ? "Generierter C-Code vollstaendig neu geladen."
                        : "Generierter C-Code aktualisiert.";
                });

                var okReply = new PreviewReplyMessage(true, "Preview-Fenster aktualisiert.");
                await writer.WriteLineAsync(JsonSerializer.Serialize(okReply, _jsonOptions));
            }
        }
    }
}
