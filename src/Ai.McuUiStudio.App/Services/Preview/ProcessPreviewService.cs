using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using Ai.McuUiStudio.App.Services;
using Ai.McuUiStudio.Core.PreviewProtocol;

namespace Ai.McuUiStudio.App.Services.Preview;

public sealed class ProcessPreviewService : IPreviewService
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    private readonly PreviewBackendKind _backendKind;
    private readonly SemaphoreSlim _sync = new(1, 1);
    private Process? _process;
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private int _port;

    public ProcessPreviewService(PreviewBackendKind backendKind = PreviewBackendKind.ManagedPreviewHost)
    {
        _backendKind = backendKind;
    }

    public event EventHandler<string>? LogReceived;

    public bool IsConnected => _process is { HasExited: false } && _client?.Connected == true;

    public string BackendName => _backendKind switch
    {
        PreviewBackendKind.NativeLvglPreview => "Native LVGL Preview (SDL)",
        _ => "C# Preview Host"
    };

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected)
            {
                return;
            }

            ResetConnectionState();

            _port = AllocatePort();
            await StartHostProcessAsync(_port, cancellationToken);
            await ConnectClientAsync(_port, cancellationToken);
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        Process? processToStop = null;

        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (_writer is not null)
            {
                var shutdown = new PreviewCommandMessage(
                    PreviewProtocolConstants.ShutdownCommand,
                    string.Empty,
                    string.Empty,
                    true,
                    null,
                    null,
                    null,
                    false);
                await _writer.WriteLineAsync(JsonSerializer.Serialize(shutdown, _jsonOptions));
                await _writer.FlushAsync();
            }

            processToStop = _process;
        }
        catch
        {
            // Best effort.
        }
        finally
        {
            ResetConnectionState();
            LogReceived?.Invoke(this, "[preview] Verbindung zum Preview-Host beendet.");
            _sync.Release();
        }

        if (processToStop is not null)
        {
            await StopProcessAsync(processToStop, cancellationToken);
        }

        _process = null;
    }

    public async Task HighlightAsync(string? objectId, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return;
        }

        await _sync.WaitAsync(cancellationToken);
        try
        {
            if (!IsConnected || _writer is null)
            {
                return;
            }

            var message = new { command = PreviewProtocolConstants.HighlightCommand, objectId = objectId ?? string.Empty };
            await _writer.WriteLineAsync(JsonSerializer.Serialize(message, _jsonOptions));
            await _writer.FlushAsync(cancellationToken);

            // Read and discard the reply
            await _reader!.ReadLineAsync(cancellationToken);
        }
        catch
        {
            // Best effort — highlight failures are non-critical.
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<PreviewRenderResult> RenderAsync(
        PreviewRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await ConnectAsync(cancellationToken);

            await _sync.WaitAsync(cancellationToken);
            try
            {
                if (!IsConnected || _writer is null || _reader is null)
                {
                    ResetConnectionState();
                    if (attempt == 0)
                    {
                        continue;
                    }

                    return new PreviewRenderResult(false, false, "Preview-Prozess nicht verbunden.");
                }

                var message = new PreviewCommandMessage(
                    request.ForceFullReload ? PreviewProtocolConstants.ReloadCommand : PreviewProtocolConstants.RenderCommand,
                    request.DocumentName,
                    request.Content,
                    request.ForceFullReload,
                    request.ScreenWidth,
                    request.ScreenHeight,
                    request.ZoomPercent,
                    request.ResetWindowToTargetSize);

                await _writer.WriteLineAsync(JsonSerializer.Serialize(message, _jsonOptions));
                await _writer.FlushAsync();

                var replyLine = await _reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(replyLine))
                {
                    LogReceived?.Invoke(this, "[preview] Preview-Host hat die Verbindung geschlossen. Neustart wird versucht.");
                    ResetConnectionState();
                    if (_process is not null && !_process.HasExited)
                    {
                        try
                        {
                            _process.Kill(entireProcessTree: true);
                            await _process.WaitForExitAsync(cancellationToken);
                        }
                        catch
                        {
                            // Best effort.
                        }
                    }

                    _process = null;
                    if (attempt == 0)
                    {
                        continue;
                    }

                    return new PreviewRenderResult(false, false, "Keine Antwort vom Preview-Host.");
                }

                var reply = JsonSerializer.Deserialize<PreviewReplyMessage>(replyLine, _jsonOptions);
                if (reply is null)
                {
                    return new PreviewRenderResult(false, true, "Antwort des Preview-Hosts war ungueltig.");
                }

                return new PreviewRenderResult(reply.Success, IsConnected, reply.StatusMessage);
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke(this, $"[preview:error] {ex.Message}");
                ResetConnectionState();
                if (_process is not null && _process.HasExited)
                {
                    _process = null;
                }

                if (attempt == 0)
                {
                    continue;
                }

                return new PreviewRenderResult(false, IsConnected, $"Preview-Fehler: {ex.Message}");
            }
            finally
            {
                _sync.Release();
            }
        }

        return new PreviewRenderResult(false, false, "Preview-Prozess nicht verbunden.");
    }

    private void ResetConnectionState()
    {
        _reader?.Dispose();
        _writer?.Dispose();
        _client?.Dispose();
        _reader = null;
        _writer = null;
        _client = null;
    }

    private async Task StartHostProcessAsync(int port, CancellationToken cancellationToken)
    {
        var repoRoot = FindRepositoryRoot();
        var psi = CreateHostProcessStartInfo(repoRoot, port);

        _process = Process.Start(psi) ?? throw new InvalidOperationException("Preview host could not be started.");
        _ = PumpLogAsync(_process.StandardOutput, isError: false, cancellationToken);
        _ = PumpLogAsync(_process.StandardError, isError: true, cancellationToken);
        LogReceived?.Invoke(this, $"[preview] {BackendName} wird auf Port {port} gestartet.");
        await Task.Delay(250, cancellationToken);
    }

    private async Task ConnectClientAsync(int port, CancellationToken cancellationToken)
    {
        const int maxAttempts = 40;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(IPAddress.Loopback, port, cancellationToken);
                var stream = _client.GetStream();
                _reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                _writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true)
                {
                    AutoFlush = true
                };
                return;
            }
            catch
            {
                _client?.Dispose();
                _client = null;
                await Task.Delay(150, cancellationToken);
            }
        }

        throw new InvalidOperationException("Preview host did not open its TCP endpoint in time.");
    }

    private async Task PumpLogAsync(StreamReader reader, bool isError, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line is null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var prefix = isError ? "[preview:stderr]" : "[preview:stdout]";
                LogReceived?.Invoke(this, $"{prefix} {line}");
            }
        }
        catch (ObjectDisposedException)
        {
            // Process stream already closed.
        }
        catch (InvalidOperationException)
        {
            // Process stream no longer available.
        }
    }

    private static int AllocatePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string? FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Ai.McuUiStudio.slnx")) ||
                File.Exists(Path.Combine(current.FullName, "MCU_UI_Studio_for_LVGL.slnx")) ||
                Directory.Exists(Path.Combine(current.FullName, "src", "Ai.McuUiStudio.App")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string ResolveDotNetExecutable()
    {
        var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
        if (!string.IsNullOrWhiteSpace(dotnetRoot))
        {
            var candidate = Path.Combine(dotnetRoot, "dotnet");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return "dotnet";
    }

    private async Task StopProcessAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            if (process.HasExited)
            {
                LogReceived?.Invoke(this, "[preview] Preview-Prozess wurde sauber beendet.");
                return;
            }

            var exitTask = process.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            var completedTask = await Task.WhenAny(exitTask, timeoutTask);
            var exited = completedTask == exitTask && process.HasExited;
            if (exited)
            {
                LogReceived?.Invoke(this, "[preview] Preview-Prozess wurde sauber beendet.");
                return;
            }

            LogReceived?.Invoke(this, "[preview] Preview-Prozess reagiert nicht auf Shutdown, Prozess wird beendet.");
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(cancellationToken);
            LogReceived?.Invoke(this, "[preview] Preview-Prozess wurde zwangsweise beendet.");
        }
        catch (InvalidOperationException)
        {
            // Process already exited.
        }
    }

    private ProcessStartInfo CreateHostProcessStartInfo(string? repoRoot, int port)
    {
        return _backendKind switch
        {
            PreviewBackendKind.NativeLvglPreview => CreateNativeHostStartInfo(repoRoot, port),
            _ => CreateManagedHostStartInfo(repoRoot, port)
        };
    }

    private static ProcessStartInfo CreateManagedHostStartInfo(string? repoRoot, int port)
    {
        if (string.IsNullOrWhiteSpace(repoRoot))
        {
            throw new InvalidOperationException(
                "Managed preview host is only available from a repository checkout. Use the native LVGL preview in packaged desktop releases.");
        }

        var projectPath = Path.Combine(
            repoRoot,
            "src",
            "Ai.McuUiStudio.PreviewHost",
            "Ai.McuUiStudio.PreviewHost.csproj");

        var psi = CreateBaseProcessStartInfo(ResolveDotNetExecutable(), repoRoot);
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(projectPath);
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("Debug");
        psi.ArgumentList.Add("--");
        psi.ArgumentList.Add("--port");
        psi.ArgumentList.Add(port.ToString());

        psi.Environment["DOTNET_CLI_HOME"] = Environment.GetEnvironmentVariable("DOTNET_CLI_HOME") ?? "/tmp/dotnet-cli";
        psi.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = Environment.GetEnvironmentVariable("DOTNET_SKIP_FIRST_TIME_EXPERIENCE") ?? "1";
        psi.Environment["AVALONIA_TELEMETRY_OPTOUT"] = Environment.GetEnvironmentVariable("AVALONIA_TELEMETRY_OPTOUT") ?? "1";
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_ROOT")))
        {
            psi.Environment["DOTNET_ROOT"] = Environment.GetEnvironmentVariable("DOTNET_ROOT")!;
        }

        return psi;
    }

    private static ProcessStartInfo CreateNativeHostStartInfo(string? repoRoot, int port)
    {
        var executablePath = ResolveNativeHostExecutable(repoRoot);
        var workingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory;
        var psi = CreateBaseProcessStartInfo(executablePath, workingDirectory);
        psi.ArgumentList.Add("--port");
        psi.ArgumentList.Add(port.ToString());
        return psi;
    }

    private static ProcessStartInfo CreateBaseProcessStartInfo(string fileName, string workingDirectory)
    {
        return new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }

    private static string ResolveNativeHostExecutable(string? repoRoot)
    {
        if (!string.IsNullOrWhiteSpace(repoRoot))
        {
            var repoCandidates = new[]
            {
                Path.Combine(repoRoot, "native", "lvgl_simulator_host", "build", "lvgl_simulator_host"),
                Path.Combine(repoRoot, "native", "lvgl_simulator_host", "build", "Debug", "lvgl_simulator_host"),
                Path.Combine(repoRoot, "native", "lvgl_simulator_host", "build", "Release", "lvgl_simulator_host"),
                Path.Combine(repoRoot, "native", "lvgl_simulator_host", "build", "lvgl_simulator_host.exe"),
                Path.Combine(repoRoot, "native", "lvgl_simulator_host", "build", "Debug", "lvgl_simulator_host.exe"),
                Path.Combine(repoRoot, "native", "lvgl_simulator_host", "build", "Release", "lvgl_simulator_host.exe")
            };

            var repoCandidate = repoCandidates.FirstOrDefault(File.Exists);
            if (!string.IsNullOrWhiteSpace(repoCandidate))
            {
                return repoCandidate;
            }
        }

        var bundledCandidate = AppRuntimePaths.TryResolveBundledNativeSimulator();
        if (!string.IsNullOrWhiteSpace(bundledCandidate))
        {
            return bundledCandidate;
        }

        throw new InvalidOperationException(
            "Native LVGL preview host not found. Build native/lvgl_simulator_host for development or use a packaged desktop release that includes the simulator.");
    }
}
