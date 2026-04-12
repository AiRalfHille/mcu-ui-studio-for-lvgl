using System.Text.Json;
using Ai.McuUiStudio.Core.MetaModel;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

public sealed class JsonGenerator
{
    private readonly LvglMetaModelRegistry _metaModelRegistry;

    public JsonGenerator()
        : this(LvglMetaModelRegistry.CreateDefault())
    {
    }

    public JsonGenerator(LvglMetaModelRegistry metaModelRegistry)
    {
        _metaModelRegistry = metaModelRegistry;
    }

    public string Generate(UiDocument document, bool includeCodeTemplateAttributes = true)
    {
        var dto = new DocumentDto
        {
            FormatVersion = 1,
            Root = CreateNode(document.Root, includeCodeTemplateAttributes)
        };

        return JsonSerializer.Serialize(dto, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    private NodeDto CreateNode(UiNode node, bool includeCodeTemplateAttributes)
    {
        var definition = ResolveElementDefinition(node.ElementName);

        return new NodeDto
        {
            Type = ResolveStorageType(node.ElementName, definition),
            Attributes = node.Attributes
                .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                .Where(x => includeCodeTemplateAttributes || ShouldEmitAttribute(definition, x.Key))
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    x => ResolveStorageAttributeName(definition, x.Key),
                    x => x.Value,
                    StringComparer.OrdinalIgnoreCase),
            Events = node.Events
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => new EventDto
                {
                    Name = x.Name,
                    Attributes = x.Attributes
                        .Where(attr => !string.IsNullOrWhiteSpace(attr.Key) && !string.IsNullOrWhiteSpace(attr.Value))
                        .OrderBy(attr => attr.Key, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(attr => attr.Key, attr => attr.Value, StringComparer.OrdinalIgnoreCase)
                })
                .Where(x => x.Attributes is { Count: > 0 })
                .ToList(),
            Children = node.Children.Select(child => CreateNode(child, includeCodeTemplateAttributes)).ToList()
        };
    }

    private LvglElementDefinition? ResolveElementDefinition(string typeName)
    {
        if (_metaModelRegistry.TryGet(typeName, out var directDefinition) && directDefinition is not null)
        {
            return directDefinition;
        }

        if (_metaModelRegistry.TryGetByLvglType(typeName, out var lvglDefinition) && lvglDefinition is not null)
        {
            return lvglDefinition;
        }

        return null;
    }

    private static string ResolveStorageType(string internalType, LvglElementDefinition? definition) =>
        definition?.Name ?? internalType;

    private static bool ShouldEmitAttribute(LvglElementDefinition? definition, string internalAttributeName)
    {
        if (definition is null)
        {
            return true;
        }

        var attribute = definition.Attributes.FirstOrDefault(x =>
            string.Equals(x.LvglName, internalAttributeName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Name, internalAttributeName, StringComparison.OrdinalIgnoreCase));

        return attribute?.Target != AttributeTarget.CodeTemplate;
    }

    private static string ResolveStorageAttributeName(LvglElementDefinition? definition, string internalAttributeName)
    {
        if (definition is null)
        {
            return internalAttributeName;
        }

        var attribute = definition.Attributes.FirstOrDefault(x =>
            string.Equals(x.LvglName, internalAttributeName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Name, internalAttributeName, StringComparison.OrdinalIgnoreCase));

        return attribute?.Name ?? internalAttributeName;
    }

    private sealed class DocumentDto
    {
        public int FormatVersion { get; init; }

        public NodeDto? Root { get; init; }
    }

    private sealed class NodeDto
    {
        public string Type { get; init; } = string.Empty;

        public Dictionary<string, string?>? Attributes { get; init; }

        public List<EventDto>? Events { get; init; }

        public List<NodeDto>? Children { get; init; }
    }

    private sealed class EventDto
    {
        public string Name { get; init; } = string.Empty;

        public Dictionary<string, string?>? Attributes { get; init; }
    }
}
