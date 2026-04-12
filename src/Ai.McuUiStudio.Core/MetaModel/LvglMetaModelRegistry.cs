namespace Ai.McuUiStudio.Core.MetaModel;

public sealed class LvglMetaModelRegistry
{
    private readonly Dictionary<string, LvglElementDefinition> _elements;
    private readonly Dictionary<string, AttributeValueTypeDefinition> _attributeTypes;
    private readonly Dictionary<string, LvglElementDefinition> _elementsByLvglType;

    public LvglMetaModelRegistry(LvglMetaModelDefinition definition)
    {
        Definition = definition;
        _elements = definition.Elements.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        _attributeTypes = definition.AttributeTypes.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        _elementsByLvglType = definition.Elements
            .Where(x => x.Targets.TryGetValue("lvgl", out var target) && !string.IsNullOrWhiteSpace(target.Type))
            .ToDictionary(
                x => x.Targets["lvgl"].Type,
                x => x,
                StringComparer.OrdinalIgnoreCase);
    }

    public LvglMetaModelDefinition Definition { get; }

    public IReadOnlyCollection<LvglElementDefinition> Elements => _elements.Values;

    public IReadOnlyCollection<AttributeValueTypeDefinition> AttributeTypes => _attributeTypes.Values;

    public IReadOnlyList<LvglElementDefinition> GetElementsByCategory(string category) =>
        _elements.Values
            .Where(x => string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public LvglElementDefinition Get(string name) => _elements[name];

    public bool TryGet(string name, out LvglElementDefinition? definition) =>
        _elements.TryGetValue(name, out definition);

    public bool TryGetByLvglType(string lvglType, out LvglElementDefinition? definition) =>
        _elementsByLvglType.TryGetValue(lvglType, out definition);

    public AttributeValueTypeDefinition GetAttributeType(string name) => _attributeTypes[name];

    public bool TryGetAttributeType(string name, out AttributeValueTypeDefinition? definition) =>
        _attributeTypes.TryGetValue(name, out definition);

    public static LvglMetaModelRegistry CreateDefault() => new(LvglMetaModelLoader.LoadEmbeddedDefault());

    public static LvglMetaModelRegistry CreateForVersion(string version) => new(LvglMetaModelLoader.LoadEmbeddedByVersion(version));
}
