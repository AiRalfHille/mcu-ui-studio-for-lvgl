using System.Net;
using System.Text;

namespace Ai.McuUiStudio.App.Services.Documentation;

public sealed class DocumentationServerService : IDisposable
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".css"] = "text/css; charset=utf-8",
            [".gif"] = "image/gif",
            [".htm"] = "text/html; charset=utf-8",
            [".html"] = "text/html; charset=utf-8",
            [".ico"] = "image/x-icon",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".js"] = "application/javascript; charset=utf-8",
            [".json"] = "application/json; charset=utf-8",
            [".map"] = "application/json; charset=utf-8",
            [".md"] = "text/markdown; charset=utf-8",
            [".png"] = "image/png",
            [".svg"] = "image/svg+xml",
            [".txt"] = "text/plain; charset=utf-8",
            [".webp"] = "image/webp",
            [".woff"] = "font/woff",
            [".woff2"] = "font/woff2",
            [".xml"] = "application/xml; charset=utf-8"
        };

    private readonly string _rootDirectory;
    private readonly CancellationTokenSource _shutdownCts = new();
    private HttpListener? _listener;
    private Task? _listenLoopTask;

    public DocumentationServerService(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
    }

    public Uri? BaseUri { get; private set; }

    public bool IsRunning => _listener is { IsListening: true };

    public string GetDocumentationUrl(string? preferredLanguageCode)
    {
        if (BaseUri is null)
        {
            return string.Empty;
        }

        foreach (var languageCode in GetLanguageCandidates(preferredLanguageCode))
        {
            if (HasLanguageSite(languageCode))
            {
                return new Uri(BaseUri, languageCode.Trim('/') + "/").ToString();
            }
        }

        return BaseUri.ToString();
    }

    public bool TryStart(out string? errorMessage)
    {
        errorMessage = null;

        if (IsRunning)
        {
            return true;
        }

        if (!Directory.Exists(_rootDirectory))
        {
            errorMessage = $"Dokumentationsverzeichnis nicht gefunden: {_rootDirectory}";
            return false;
        }

        try
        {
            var port = GetFreeTcpPort();
            var prefix = $"http://127.0.0.1:{port}/";
            var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            _listener = listener;
            BaseUri = new Uri(prefix, UriKind.Absolute);
            _listenLoopTask = Task.Run(() => ListenLoopAsync(listener, _shutdownCts.Token));
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }

    public void Dispose()
    {
        _shutdownCts.Cancel();

        try
        {
            _listener?.Stop();
        }
        catch
        {
            // Ignore shutdown errors during app exit.
        }

        try
        {
            _listener?.Close();
        }
        catch
        {
            // Ignore shutdown errors during app exit.
        }

        _listener = null;
        BaseUri = null;
    }

    private async Task ListenLoopAsync(HttpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && listener.IsListening)
        {
            HttpListenerContext? context = null;

            try
            {
                context = await listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            if (context is null)
            {
                continue;
            }

            _ = Task.Run(() => ProcessRequestAsync(context, cancellationToken), cancellationToken);
        }
    }

    private async Task ProcessRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            var relativePath = GetRelativePath(context.Request.Url);
            var fullPath = ResolveFilePath(relativePath);

            if (fullPath is null || !File.Exists(fullPath))
            {
                await WriteNotFoundAsync(context.Response, cancellationToken);
                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = GetContentType(fullPath);

            await using var input = File.OpenRead(fullPath);
            context.Response.ContentLength64 = input.Length;
            await input.CopyToAsync(context.Response.OutputStream, cancellationToken);
            context.Response.OutputStream.Close();
        }
        catch (Exception)
        {
            try
            {
                if (context.Response.OutputStream.CanWrite)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.OutputStream.Close();
                }
            }
            catch
            {
                // Ignore follow-up response errors.
            }
        }
    }

    private string GetRelativePath(Uri? requestUri)
    {
        if (requestUri is null)
        {
            return "index.html";
        }

        var path = Uri.UnescapeDataString(requestUri.AbsolutePath.TrimStart('/'));
        if (string.IsNullOrWhiteSpace(path))
        {
            return "index.html";
        }

        if (path.EndsWith('/'))
        {
            return path + "index.html";
        }

        return path;
    }

    private string? ResolveFilePath(string relativePath)
    {
        var normalizedPath = relativePath.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        var candidatePath = Path.GetFullPath(Path.Combine(_rootDirectory, normalizedPath));
        var rootPath = Path.GetFullPath(_rootDirectory);

        if (!candidatePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (Directory.Exists(candidatePath))
        {
            candidatePath = Path.Combine(candidatePath, "index.html");
        }

        return candidatePath;
    }

    private static string GetContentType(string path)
    {
        var extension = Path.GetExtension(path);
        return ContentTypes.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    private static async Task WriteNotFoundAsync(HttpListenerResponse response, CancellationToken cancellationToken)
    {
        var payload = Encoding.UTF8.GetBytes("Not Found");
        response.StatusCode = (int)HttpStatusCode.NotFound;
        response.ContentType = "text/plain; charset=utf-8";
        response.ContentLength64 = payload.Length;
        await response.OutputStream.WriteAsync(payload, cancellationToken);
        response.OutputStream.Close();
    }

    private static int GetFreeTcpPort()
    {
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    private IEnumerable<string> GetLanguageCandidates(string? preferredLanguageCode)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(preferredLanguageCode))
        {
            var normalized = preferredLanguageCode.Trim().Trim('/').Replace('\\', '/');
            if (seen.Add(normalized))
            {
                yield return normalized;
            }

            var separatorIndex = normalized.IndexOfAny(['-', '_']);
            if (separatorIndex > 0)
            {
                var baseLanguage = normalized[..separatorIndex];
                if (seen.Add(baseLanguage))
                {
                    yield return baseLanguage;
                }
            }
        }

        if (seen.Add("en"))
        {
            yield return "en";
        }

        if (seen.Add("de"))
        {
            yield return "de";
        }
    }

    private bool HasLanguageSite(string languageCode)
    {
        var languageRoot = Path.Combine(_rootDirectory, languageCode);
        return Directory.Exists(languageRoot) &&
               File.Exists(Path.Combine(languageRoot, "index.html"));
    }
}
