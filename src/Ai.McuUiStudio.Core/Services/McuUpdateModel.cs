using System.Text.RegularExpressions;
using Ai.McuUiStudio.Core.MetaModel;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

internal enum McuUpdatePropertyKind
{
    Text,
    Value,
    Checked,
    Visible,
    Enabled,
    TextColor,
    BackgroundColor,
    Font
}

internal sealed record McuUpdateTarget(
    string ObjectName,
    string VariableName,
    string DefinitionName,
    IReadOnlyList<McuUpdatePropertyKind> Properties);

internal static class McuUpdateModel
{
    public static IReadOnlyList<McuUpdateTarget> CollectTargets(UiDocument document, LvglMetaModelRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(registry);

        var targets = new List<McuUpdateTarget>();
        CollectTargets(document.Root, registry, targets);
        return targets;
    }

    public static string CreateObjectHandleName(string unitName, string objectName) =>
        SanitizeIdentifier(objectName, "obj");

    public static string CreateTargetEnumName(string unitName, string objectName, McuUpdatePropertyKind property) =>
        $"{SanitizeIdentifier(unitName, "ui_start").ToUpperInvariant()}_UPDATE_TARGET_{SanitizeIdentifier(objectName, "obj").ToUpperInvariant()}_{ToEnumSuffix(property)}";

    public static string CreateWrapperFunctionName(string unitName, string objectName, McuUpdatePropertyKind property) =>
        $"{SanitizeIdentifier(unitName, "ui_start")}_set_{SanitizeIdentifier(objectName, "obj")}_{ToSnakeCase(property)}";

    private static void CollectTargets(UiNode node, LvglMetaModelRegistry registry, ICollection<McuUpdateTarget> targets)
    {
        var definition = ResolveElementDefinition(node.ElementName, registry);
        if (definition is not null)
        {
            var useUpdate = GetAttributeValue(node.Attributes, "useUpdate");
            if (string.Equals(useUpdate, "true", StringComparison.OrdinalIgnoreCase))
            {
                var objectName = GetAttributeValue(node.Attributes, "id", "name");
                if (!string.IsNullOrWhiteSpace(objectName))
                {
                    var properties = GetCoreProperties(definition.Name);
                    if (properties.Count > 0)
                    {
                        targets.Add(new McuUpdateTarget(
                            objectName,
                            SanitizeIdentifier(objectName, definition.Name),
                            definition.Name,
                            properties));
                    }
                }
            }
        }

        foreach (var child in node.Children)
        {
            CollectTargets(child, registry, targets);
        }
    }

    private static IReadOnlyList<McuUpdatePropertyKind> GetCoreProperties(string definitionName)
    {
        var properties = new List<McuUpdatePropertyKind>
        {
            McuUpdatePropertyKind.Visible,
            McuUpdatePropertyKind.Enabled,
            McuUpdatePropertyKind.TextColor,
            McuUpdatePropertyKind.BackgroundColor,
            McuUpdatePropertyKind.Font
        };

        if (SupportsText(definitionName))
        {
            properties.Insert(0, McuUpdatePropertyKind.Text);
        }

        if (SupportsValue(definitionName))
        {
            properties.Insert(0, McuUpdatePropertyKind.Value);
        }

        if (SupportsChecked(definitionName))
        {
            properties.Insert(0, McuUpdatePropertyKind.Checked);
        }

        return properties;
    }

    private static bool SupportsText(string definitionName) =>
        definitionName switch
        {
            "label" => true,
            "button" => true,
            "checkbox" => true,
            "textarea" => true,
            _ => false
        };

    private static bool SupportsValue(string definitionName) =>
        definitionName switch
        {
            "bar" => true,
            "slider" => true,
            "arc" => true,
            "spinbox" => true,
            _ => false
        };

    private static bool SupportsChecked(string definitionName) =>
        definitionName switch
        {
            "checkbox" => true,
            "switch" => true,
            _ => false
        };

    private static LvglElementDefinition? ResolveElementDefinition(string typeName, LvglMetaModelRegistry registry)
    {
        if (registry.TryGet(typeName, out var directDefinition) && directDefinition is not null)
        {
            return directDefinition;
        }

        if (registry.TryGetByLvglType(typeName, out var lvglDefinition) && lvglDefinition is not null)
        {
            return lvglDefinition;
        }

        return null;
    }

    private static string? GetAttributeValue(IDictionary<string, string?> attributes, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (attributes.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string SanitizeIdentifier(string? value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        normalized = Regex.Replace(normalized, @"[^A-Za-z0-9_]", "_");
        if (char.IsDigit(normalized[0]))
        {
            normalized = $"_{normalized}";
        }

        return normalized;
    }

    private static string ToSnakeCase(McuUpdatePropertyKind property) =>
        property switch
        {
            McuUpdatePropertyKind.Text => "text",
            McuUpdatePropertyKind.Value => "value",
            McuUpdatePropertyKind.Checked => "checked",
            McuUpdatePropertyKind.Visible => "visible",
            McuUpdatePropertyKind.Enabled => "enabled",
            McuUpdatePropertyKind.TextColor => "text_color",
            McuUpdatePropertyKind.BackgroundColor => "background_color",
            McuUpdatePropertyKind.Font => "font",
            _ => "value"
        };

    private static string ToEnumSuffix(McuUpdatePropertyKind property) =>
        property switch
        {
            McuUpdatePropertyKind.Text => "TEXT",
            McuUpdatePropertyKind.Value => "VALUE",
            McuUpdatePropertyKind.Checked => "CHECKED",
            McuUpdatePropertyKind.Visible => "VISIBLE",
            McuUpdatePropertyKind.Enabled => "ENABLED",
            McuUpdatePropertyKind.TextColor => "TEXT_COLOR",
            McuUpdatePropertyKind.BackgroundColor => "BACKGROUND_COLOR",
            McuUpdatePropertyKind.Font => "FONT",
            _ => "VALUE"
        };
}
