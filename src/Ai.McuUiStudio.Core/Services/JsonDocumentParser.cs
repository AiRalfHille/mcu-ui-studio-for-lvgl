using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

public sealed class JsonDocumentParser
{
    private readonly JsonDocumentSerializer _serializer = new();

    public UiDocument Parse(string json) => _serializer.Deserialize(json);
}
