using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.MetaModel;

public static class LvglMetaModelLoader
{
    private const string ResourcePrefix = "Ai.McuUiStudio.Core.MetaModel.";
    private const string DefaultVersionKey = "9.4";
    private const string UseUpdateAttributeName = "useUpdate";
    private const string McuActionTypeName = "mcu_action";
    private static readonly Regex EmbeddedVersionRegex = new(
        @"^Ai\.McuUiStudio\.Core\.MetaModel\.ui-lvgl-(?<version>\d+\.\d+)\.metamodel\.v2\.json$",
        RegexOptions.Compiled);

    public static string DefaultVersion => DefaultVersionKey;

    public static LvglMetaModelDefinition LoadEmbeddedDefault() => LoadEmbeddedByVersion(DefaultVersionKey);

    public static IReadOnlyList<string> GetEmbeddedVersions() =>
        Assembly.GetExecutingAssembly()
            .GetManifestResourceNames()
            .Select(TryExtractVersion)
            .Where(x => x is not null)
            .Cast<string>()
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static LvglMetaModelDefinition LoadEmbeddedByVersion(string version)
    {
        var embeddedResourceName = $"{ResourcePrefix}ui-lvgl-{version}.metamodel.v2.json";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Embedded V2 metamodel resource '{embeddedResourceName}' was not found.");
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return LoadFromJson(json);
    }

    public static LvglMetaModelDefinition LoadFromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        var dto = JsonSerializer.Deserialize<LvglMetaModelDefinitionDto>(json, options)
                  ?? throw new InvalidOperationException("V2 metamodel JSON could not be deserialized.");

        var attributeTypes = dto.AttributeTypes
            .Select(x => new AttributeValueTypeDefinition(
                x.Name,
                x.Kind,
                x.AllowedValues ?? []))
            .Concat(CreateAdditionalAttributeTypes(dto))
            .ToArray();

        var elements = dto.Elements
            .Select(x =>
            {
                var attributes = x.Attributes.Select(a => new LvglElementAttributeDefinition(
                        a.Name,
                        a.DisplayName ?? a.Name,
                        a.LvglName ?? a.Name,
                        a.Type,
                        a.Required,
                        a.AllowedValues,
                        ParseAttributeTarget(a.Target),
                        a.Supported))
                    .ToList();

                AppendSharedCodeTemplateAttributes(attributes);

                return new LvglElementDefinition(
                    x.Name,
                    x.DisplayName ?? x.Name,
                    x.Category ?? "General",
                    x.Kind ?? "simple",
                    (x.Targets ?? new Dictionary<string, LvglElementTargetDto>(StringComparer.OrdinalIgnoreCase))
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => new LvglElementTargetDefinition(kvp.Value.Type),
                            StringComparer.OrdinalIgnoreCase),
                    attributes.ToArray(),
                    CreateEventDefinitions(x.Events),
                    new ElementChildrenDefinition(
                        x.Children?.Allowed ?? [],
                        x.Children?.Min ?? 0,
                        x.Children?.Max),
                    x.Supported);
            })
            .ToArray();

        return new LvglMetaModelDefinition(
            dto.Name,
            dto.Version,
            dto.Targets ?? [],
            attributeTypes,
            elements);
    }

    private static AttributeTarget ParseAttributeTarget(string? value)
    {
        if (string.Equals(value, "code-template", StringComparison.OrdinalIgnoreCase))
        {
            return AttributeTarget.CodeTemplate;
        }

        return AttributeTarget.Display;
    }

    private static IEnumerable<AttributeValueTypeDefinition> CreateAdditionalAttributeTypes(LvglMetaModelDefinitionDto dto)
    {
        if (!dto.AttributeTypes.Any(type => string.Equals(type.Name, McuActionTypeName, StringComparison.OrdinalIgnoreCase)))
        {
            yield return CreateMcuActionType();
        }
    }

    private static AttributeValueTypeDefinition CreateMcuActionType() =>
        new(
            McuActionTypeName,
            AttributeType.Enum,
            [
                "ACTION_BACKWARD",
                "ACTION_FORWARD",
                "ACTION_STOP",
                "ACTION_SPEED",
                "ACTION_START",
                "ACTION_TOGGLE",
                "ACTION_OPEN",
                "ACTION_CLOSE",
                "ACTION_ENABLE",
                "ACTION_DISABLE",
                "ACTION_SELECT",
                "ACTION_NEXT",
                "ACTION_PREVIOUS",
                "ACTION_INCREASE",
                "ACTION_DECREASE",
                "ACTION_RESET",
                "ACTION_CONFIRM",
                "ACTION_CANCEL",
                "ACTION_SUBMIT"
            ]);

    private static IReadOnlyList<LvglEventDefinition> CreateEventDefinitions(List<LvglEventDto>? events)
    {
        if (events is null || events.Count == 0)
        {
            return [];
        }

        return events
            .Select(evt => new LvglEventDefinition(
                evt.Name,
                evt.DisplayName ?? CreateEventDisplayName(evt.Name),
                evt.Attributes?.Select(attribute => new LvglElementAttributeDefinition(
                        attribute.Name,
                        attribute.DisplayName ?? attribute.Name,
                        attribute.LvglName ?? attribute.Name,
                        attribute.Type,
                        attribute.Required,
                        attribute.AllowedValues,
                        ParseAttributeTarget(attribute.Target),
                        attribute.Supported))
                    .ToArray()
                ?? CreateDefaultEventAttributes()))
            .ToArray();
    }

    private static IReadOnlyList<LvglElementAttributeDefinition> CreateDefaultEventAttributes() =>
    [
        new LvglElementAttributeDefinition("callback", "Callback", "callback", "event"),
        new LvglElementAttributeDefinition("action", "Action", "action", McuActionTypeName, false, null, AttributeTarget.CodeTemplate, true),
        new LvglElementAttributeDefinition("parameter", "Parameter", "parameter", "string", false, null, AttributeTarget.CodeTemplate, true),
        new LvglElementAttributeDefinition("eventGroup", "Event Group", "eventGroup", "string", false, null, AttributeTarget.CodeTemplate, true),
        new LvglElementAttributeDefinition("eventType", "Event Type", "eventType", "string", false, null, AttributeTarget.CodeTemplate, true),
        new LvglElementAttributeDefinition("useMessages", "Use Messages", "useMessages", "boolean", false, null, AttributeTarget.CodeTemplate, true)
    ];

    private static string CreateEventDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Event";
        }

        return string.Join(
            " ",
            name.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static void AppendSharedCodeTemplateAttributes(ICollection<LvglElementAttributeDefinition> attributes)
    {
        if (attributes.Any(attribute =>
                string.Equals(attribute.Name, UseUpdateAttributeName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(attribute.LvglName, UseUpdateAttributeName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        attributes.Add(new LvglElementAttributeDefinition(
            UseUpdateAttributeName,
            "Use Update",
            UseUpdateAttributeName,
            "boolean",
            false,
            null,
            AttributeTarget.CodeTemplate,
            true));
    }

    private static string? TryExtractVersion(string resourceName)
    {
        var match = EmbeddedVersionRegex.Match(resourceName);
        return match.Success ? match.Groups["version"].Value : null;
    }

    private sealed class LvglMetaModelDefinitionDto
    {
        public string Name { get; init; } = string.Empty;

        public string Version { get; init; } = string.Empty;

        public List<string>? Targets { get; init; }

        public List<AttributeTypeDto> AttributeTypes { get; init; } = [];

        public List<LvglElementDto> Elements { get; init; } = [];
    }

    private sealed class AttributeTypeDto
    {
        public string Name { get; init; } = string.Empty;

        public AttributeType Kind { get; init; }

        public List<string>? AllowedValues { get; init; }
    }

    private sealed class LvglElementDto
    {
        public string Name { get; init; } = string.Empty;

        public string? DisplayName { get; init; }

        public string? Category { get; init; }

        public string? Kind { get; init; }

        public Dictionary<string, LvglElementTargetDto>? Targets { get; init; }

        public List<LvglElementAttributeDto> Attributes { get; init; } = [];

        [JsonConverter(typeof(LvglEventListJsonConverter))]
        public List<LvglEventDto>? Events { get; init; }

        public ElementChildrenDto? Children { get; init; }

        public bool Supported { get; init; } = true;
    }

    private sealed class LvglElementTargetDto
    {
        public string Type { get; init; } = string.Empty;
    }

    private sealed class LvglElementAttributeDto
    {
        public string Name { get; init; } = string.Empty;

        public string? DisplayName { get; init; }

        public string? LvglName { get; init; }

        public string Type { get; init; } = string.Empty;

        public bool Required { get; init; }

        public List<string>? AllowedValues { get; init; }

        public string? Target { get; init; }

        public bool Supported { get; init; } = true;
    }

    private sealed class LvglEventDto
    {
        public string Name { get; init; } = string.Empty;

        public string? DisplayName { get; init; }

        public List<LvglElementAttributeDto>? Attributes { get; init; }
    }

    private sealed class ElementChildrenDto
    {
        public List<string>? Allowed { get; init; }

        public int Min { get; init; }

        public int? Max { get; init; }
    }

    private sealed class LvglEventListJsonConverter : JsonConverter<List<LvglEventDto>>
    {
        public override List<LvglEventDto> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var result = new List<LvglEventDto>();
            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var name = item.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        result.Add(new LvglEventDto { Name = name });
                    }
                }
                else if (item.ValueKind == JsonValueKind.Object)
                {
                    var dto = JsonSerializer.Deserialize<LvglEventDto>(item.GetRawText(), options);
                    if (dto is not null && !string.IsNullOrWhiteSpace(dto.Name))
                    {
                        result.Add(dto);
                    }
                }
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, List<LvglEventDto> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var item in value)
            {
                JsonSerializer.Serialize(writer, item, options);
            }
            writer.WriteEndArray();
        }
    }
}
