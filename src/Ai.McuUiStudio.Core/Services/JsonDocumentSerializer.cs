using System.Text.Json;
using Ai.McuUiStudio.Core.MetaModel;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

public sealed class JsonDocumentSerializer
{
    private readonly LvglMetaModelRegistry _metaModelRegistry = LvglMetaModelRegistry.CreateDefault();
    private readonly JsonGenerator _jsonGenerator;

    public JsonDocumentSerializer()
    {
        _jsonGenerator = new JsonGenerator(_metaModelRegistry);
    }

    public UiDocument Deserialize(string json)
    {
        var dto = JsonSerializer.Deserialize<DocumentDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("JSON-Dokument konnte nicht gelesen werden.");

        if (dto.Root is null)
        {
            throw new InvalidOperationException("JSON-Dokument enthaelt kein Root-Element.");
        }

        return new UiDocument(ParseNode(dto.Root));
    }

    public string Serialize(UiDocument document)
        => _jsonGenerator.Generate(document);

    private UiNode ParseNode(NodeDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Type))
        {
            throw new InvalidOperationException("Ein Dokumentknoten enthaelt keinen Typ.");
        }

        var node = new UiNode(ResolveStorageTypeToInternal(dto.Type));
        var definition = ResolveStorageTypeToDefinition(dto.Type);
        var legacyEventMap = new Dictionary<string, UiEventBinding>(StringComparer.OrdinalIgnoreCase);

        foreach (var attribute in dto.Attributes ?? new Dictionary<string, string?>())
        {
            if (!string.IsNullOrWhiteSpace(attribute.Key) && !string.IsNullOrWhiteSpace(attribute.Value))
            {
                if (TryMapLegacyEventAttribute(attribute.Key, attribute.Value!, legacyEventMap))
                {
                    continue;
                }

                node.Attributes[ResolveStorageAttributeNameToInternal(definition, attribute.Key)] = attribute.Value;
            }
        }

        foreach (var legacyEvent in legacyEventMap.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            node.Events.Add(legacyEvent);
        }

        foreach (var eventDto in dto.Events ?? [])
        {
            if (string.IsNullOrWhiteSpace(eventDto.Name))
            {
                continue;
            }

            var binding = new UiEventBinding(eventDto.Name);
            foreach (var attribute in eventDto.Attributes ?? new Dictionary<string, string?>())
            {
                if (!string.IsNullOrWhiteSpace(attribute.Key) && !string.IsNullOrWhiteSpace(attribute.Value))
                {
                    binding.Attributes[attribute.Key] = attribute.Value;
                }
            }

            node.Events.RemoveAll(x => string.Equals(x.Name, binding.Name, StringComparison.OrdinalIgnoreCase));
            node.Events.Add(binding);
        }

        foreach (var child in dto.Children ?? [])
        {
            node.Children.Add(ParseNode(child));
        }

        return node;
    }

    private string ResolveStorageTypeToInternal(string storageType)
    {
        if (_metaModelRegistry.TryGet(storageType, out var definition) &&
            definition is not null &&
            definition.Targets.TryGetValue("lvgl", out var target) &&
            !string.IsNullOrWhiteSpace(target.Type))
        {
            return target.Type;
        }

        return storageType;
    }

    private LvglElementDefinition? ResolveStorageTypeToDefinition(string storageType)
    {
        if (_metaModelRegistry.TryGet(storageType, out var directDefinition) && directDefinition is not null)
        {
            return directDefinition;
        }

        if (_metaModelRegistry.TryGetByLvglType(storageType, out var lvglDefinition) && lvglDefinition is not null)
        {
            return lvglDefinition;
        }

        return null;
    }

    private static string ResolveStorageAttributeNameToInternal(LvglElementDefinition? definition, string storageAttributeName)
    {
        if (definition is null)
        {
            return storageAttributeName;
        }

        var attributeDefinition = definition.Attributes.FirstOrDefault(x =>
            string.Equals(x.Name, storageAttributeName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.LvglName, storageAttributeName, StringComparison.OrdinalIgnoreCase));

        return attributeDefinition?.LvglName ?? storageAttributeName;
    }

    private static bool TryMapLegacyEventAttribute(string attributeName, string value, IDictionary<string, UiEventBinding> eventMap)
    {
        if (attributeName.StartsWith("on_", StringComparison.OrdinalIgnoreCase))
        {
            var eventName = attributeName["on_".Length..];
            var binding = GetOrCreateEvent(eventMap, eventName);
            binding.Attributes["callback"] = value;
            return true;
        }

        if (attributeName.StartsWith("action_on_", StringComparison.OrdinalIgnoreCase))
        {
            var eventName = attributeName["action_on_".Length..];
            var binding = GetOrCreateEvent(eventMap, eventName);
            binding.Attributes["action"] = value;
            return true;
        }

        if (attributeName.StartsWith("parameter_on_", StringComparison.OrdinalIgnoreCase))
        {
            var eventName = attributeName["parameter_on_".Length..];
            var binding = GetOrCreateEvent(eventMap, eventName);
            binding.Attributes["parameter"] = value;
            return true;
        }

        return false;
    }

    private static UiEventBinding GetOrCreateEvent(IDictionary<string, UiEventBinding> eventMap, string eventName)
    {
        if (!eventMap.TryGetValue(eventName, out var binding))
        {
            binding = new UiEventBinding(eventName);
            eventMap[eventName] = binding;
        }

        return binding;
    }

    private sealed class DocumentDto
    {
        public int FormatVersion { get; init; } = 1;

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
