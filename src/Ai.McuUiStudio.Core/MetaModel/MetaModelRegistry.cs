using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.MetaModel;

public sealed class MetaModelRegistry
{
    private readonly Dictionary<string, ElementDefinition> _elements;
    private readonly Dictionary<string, AttributeValueTypeDefinition> _attributeTypes;

    public MetaModelRegistry(MetaModelDefinition definition)
    {
        Definition = definition;
        _elements = definition.Elements.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        _attributeTypes = definition.AttributeTypes.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    public MetaModelDefinition Definition { get; }

    public IReadOnlyCollection<ElementDefinition> Elements => _elements.Values;

    public IReadOnlyCollection<AttributeValueTypeDefinition> AttributeTypes => _attributeTypes.Values;

    public IReadOnlyList<ElementDefinition> GetElementsByCategory(string category) =>
        _elements.Values
            .Where(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public ElementDefinition Get(string name) => _elements[name];

    public bool TryGet(string name, out ElementDefinition? definition) =>
        _elements.TryGetValue(name, out definition);

    public AttributeValueTypeDefinition GetAttributeType(string name) => _attributeTypes[name];

    public bool TryGetAttributeType(string name, out AttributeValueTypeDefinition? definition) =>
        _attributeTypes.TryGetValue(name, out definition);

    public static MetaModelRegistry CreateDefault() => new(MetaModelLoader.LoadEmbeddedDefault());

    public static MetaModelRegistry CreateForVersion(string version) => new(MetaModelLoader.LoadEmbeddedByVersion(version));
}
