using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.MetaModel;

public static class MetaModelLoader
{
    private const string ResourcePrefix = "Ai.McuUiStudio.Core.MetaModel.";
    private const string DefaultVersionKey = "9.4";
    private static readonly Regex EmbeddedVersionRegex = new(
        @"^Ai\.McuUiStudio\.Core\.MetaModel\.lvgl-(?<version>\d+\.\d+)\.metamodel\.json$",
        RegexOptions.Compiled);

    public static string DefaultVersion => DefaultVersionKey;

    public static MetaModelDefinition LoadEmbeddedDefault() => LoadEmbeddedByVersion(DefaultVersionKey);

    public static IReadOnlyList<string> GetEmbeddedVersions() =>
        Assembly.GetExecutingAssembly()
            .GetManifestResourceNames()
            .Select(TryExtractVersion)
            .Where(x => x is not null)
            .Cast<string>()
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static MetaModelDefinition LoadEmbeddedByVersion(string version)
    {
        var embeddedResourceName = $"{ResourcePrefix}lvgl-{version}.metamodel.json";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedResourceName);
        if (stream is null)
        {
            throw new InvalidOperationException($"Embedded metamodel resource '{embeddedResourceName}' was not found.");
        }

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        return LoadFromJson(json);
    }

    public static MetaModelDefinition LoadFromJson(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());

        var dto = JsonSerializer.Deserialize<MetaModelDefinitionDto>(json, options)
                  ?? throw new InvalidOperationException("Metamodel JSON could not be deserialized.");

        var attributeTypes = dto.AttributeTypes
            .Select(x => new AttributeValueTypeDefinition(
                x.Name,
                x.Kind,
                x.AllowedValues ?? []))
            .ToArray();

        var elements = dto.Elements
            .Select(x => new ElementDefinition(
                x.Name,
                x.DisplayName ?? x.Name,
                x.Category ?? "General",
                x.Attributes.Select(a => new ElementAttributeDefinition(
                    a.Name,
                    a.Type,
                    a.Required,
                    a.AllowedValues,
                    ParseAttributeTarget(a.Target))).ToArray(),
                new ElementChildrenDefinition(
                    x.Children?.Allowed ?? [],
                    x.Children?.Min ?? 0,
                    x.Children?.Max)))
            .ToArray();

        return new MetaModelDefinition(
            dto.Name,
            dto.Version,
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

    private static string? TryExtractVersion(string resourceName)
    {
        var match = EmbeddedVersionRegex.Match(resourceName);
        return match.Success ? match.Groups["version"].Value : null;
    }

    private sealed class MetaModelDefinitionDto
    {
        public string Name { get; init; } = string.Empty;

        public string Version { get; init; } = string.Empty;

        public List<AttributeTypeDto> AttributeTypes { get; init; } = [];

        public List<ElementDto> Elements { get; init; } = [];
    }

    private sealed class AttributeTypeDto
    {
        public string Name { get; init; } = string.Empty;

        public AttributeType Kind { get; init; }

        public List<string>? AllowedValues { get; init; }
    }

    private sealed class ElementDto
    {
        public string Name { get; init; } = string.Empty;

        public string? DisplayName { get; init; }

        public string? Category { get; init; }

        public List<ElementAttributeDto> Attributes { get; init; } = [];

        public ElementChildrenDto? Children { get; init; }
    }

    private sealed class ElementAttributeDto
    {
        public string Name { get; init; } = string.Empty;

        public string Type { get; init; } = string.Empty;

        public bool Required { get; init; }

        public List<string>? AllowedValues { get; init; }

        public string? Target { get; init; }
    }

    private sealed class ElementChildrenDto
    {
        public List<string>? Allowed { get; init; }

        public int Min { get; init; }

        public int? Max { get; init; }
    }
}
