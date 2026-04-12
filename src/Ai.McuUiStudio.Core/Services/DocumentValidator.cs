using Ai.McuUiStudio.Core.MetaModel;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

public sealed class DocumentValidator
{
    private readonly LvglMetaModelRegistry _registry;

    public DocumentValidator(LvglMetaModelRegistry registry)
    {
        _registry = registry;
    }

    public ValidationResult Validate(UiDocument document, bool strictValidation)
        => Validate(document, strictValidation, null);

    public ValidationResult Validate(UiDocument document, bool strictValidation, string? projectTemplate)
    {
        var result = new ValidationResult();
        ValidateNode(document.Root, null, strictValidation, result);

        if (string.Equals(projectTemplate, "RTOS-Messages", StringComparison.OrdinalIgnoreCase))
        {
            ValidateRtosMessagesContract(document, result);
        }

        return result;
    }

    private void ValidateNode(
        UiNode node,
        UiNode? parent,
        bool strictValidation,
        ValidationResult result)
    {
        var definition = ResolveElement(node.ElementName);
        if (definition is null)
        {
            result.Errors.Add($"Unknown node type '{node.ElementName}'.");
            return;
        }

        if (parent is not null)
        {
            ValidateParentChildRelationship(parent, node, definition, result);
        }

        if (strictValidation)
        {
            foreach (var attribute in definition.Attributes.Where(x => x.IsRequired))
            {
                if (!node.Attributes.TryGetValue(attribute.LvglName, out var value) &&
                    !node.Attributes.TryGetValue(attribute.Name, out value) ||
                    string.IsNullOrWhiteSpace(value))
                {
                    result.Errors.Add(
                        $"Node '{DisplayNodeType(node.ElementName, definition)}' is missing required property '{attribute.Name}'.");
                }
            }
        }

        foreach (var providedAttribute in node.Attributes.Keys)
        {
            var attributeDefinition = definition.Attributes
                .FirstOrDefault(x =>
                    string.Equals(x.Name, providedAttribute, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.LvglName, providedAttribute, StringComparison.OrdinalIgnoreCase));

            if (attributeDefinition is null)
            {
                result.Errors.Add(
                    $"Node '{DisplayNodeType(node.ElementName, definition)}' does not support property '{providedAttribute}'.");
                continue;
            }

            if (!_registry.TryGetAttributeType(attributeDefinition.TypeName, out var typeDefinition) || typeDefinition is null)
            {
                result.Errors.Add(
                    $"Node '{DisplayNodeType(node.ElementName, definition)}' references unknown property type '{attributeDefinition.TypeName}'.");
                continue;
            }

            var allowedValues = attributeDefinition.AllowedValues?.Count > 0
                ? attributeDefinition.AllowedValues
                : typeDefinition.AllowedValues;

            if (allowedValues.Count > 0 &&
                node.Attributes.TryGetValue(providedAttribute, out var value) &&
                !string.IsNullOrWhiteSpace(value) &&
                !IsAllowedValue(typeDefinition, allowedValues, value))
            {
                result.Errors.Add(
                    $"Property '{providedAttribute}' on node '{DisplayNodeType(node.ElementName, definition)}' has invalid value '{value}'.");
                continue;
            }

            if (node.Attributes.TryGetValue(providedAttribute, out var typedValue) &&
                !string.IsNullOrWhiteSpace(typedValue) &&
                !IsValidValue(typeDefinition, typedValue))
            {
                result.Errors.Add(
                    $"Property '{providedAttribute}' on node '{DisplayNodeType(node.ElementName, definition)}' has invalid value '{typedValue}'.");
                continue;
            }

            if (IsEventAttribute(providedAttribute, attributeDefinition) &&
                !IsSupportedEvent(definition, providedAttribute))
            {
                result.Errors.Add(
                    $"Property '{providedAttribute}' on node '{DisplayNodeType(node.ElementName, definition)}' is not declared in the event model.");
            }
        }

        foreach (var evt in node.Events)
        {
            var eventDefinition = definition.Events.FirstOrDefault(x =>
                string.Equals(x.Name, evt.Name, StringComparison.OrdinalIgnoreCase));

            if (eventDefinition is null)
            {
                result.Errors.Add(
                    $"Node '{DisplayNodeType(node.ElementName, definition)}' does not support event '{evt.Name}'.");
                continue;
            }

            foreach (var providedAttribute in evt.Attributes.Keys)
            {
                var attributeDefinition = eventDefinition.Attributes.FirstOrDefault(x =>
                    string.Equals(x.Name, providedAttribute, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.LvglName, providedAttribute, StringComparison.OrdinalIgnoreCase));

                if (attributeDefinition is null)
                {
                    result.Errors.Add(
                        $"Event '{evt.Name}' on node '{DisplayNodeType(node.ElementName, definition)}' does not support property '{providedAttribute}'.");
                    continue;
                }

                if (!_registry.TryGetAttributeType(attributeDefinition.TypeName, out var typeDefinition) || typeDefinition is null)
                {
                    result.Errors.Add(
                        $"Event '{evt.Name}' on node '{DisplayNodeType(node.ElementName, definition)}' references unknown property type '{attributeDefinition.TypeName}'.");
                    continue;
                }

                var allowedValues = attributeDefinition.AllowedValues?.Count > 0
                    ? attributeDefinition.AllowedValues
                    : typeDefinition.AllowedValues;

                if (evt.Attributes.TryGetValue(providedAttribute, out var value) &&
                    !string.IsNullOrWhiteSpace(value) &&
                    allowedValues.Count > 0 &&
                    !IsAllowedValue(typeDefinition, allowedValues, value))
                {
                    result.Errors.Add(
                        $"Property '{providedAttribute}' on event '{evt.Name}' of node '{DisplayNodeType(node.ElementName, definition)}' has invalid value '{value}'.");
                    continue;
                }

                if (evt.Attributes.TryGetValue(providedAttribute, out var typedValue) &&
                    !string.IsNullOrWhiteSpace(typedValue) &&
                    !IsValidValue(typeDefinition, typedValue))
                {
                    result.Errors.Add(
                        $"Property '{providedAttribute}' on event '{evt.Name}' of node '{DisplayNodeType(node.ElementName, definition)}' has invalid value '{typedValue}'.");
                }
            }
        }

        if (node.Children.Count < definition.Children.Min)
        {
            result.Errors.Add(
                $"Node '{DisplayNodeType(node.ElementName, definition)}' requires at least {definition.Children.Min} child node(s).");
        }

        if (definition.Children.Max.HasValue && node.Children.Count > definition.Children.Max.Value)
        {
            result.Errors.Add(
                $"Node '{DisplayNodeType(node.ElementName, definition)}' allows at most {definition.Children.Max.Value} child node(s).");
        }

        foreach (var child in node.Children)
        {
            ValidateNode(child, node, strictValidation, result);
        }
    }

    private void ValidateParentChildRelationship(
        UiNode parent,
        UiNode child,
        LvglElementDefinition childDefinition,
        ValidationResult result)
    {
        var parentDefinition = ResolveElement(parent.ElementName);
        if (parentDefinition is null)
        {
            return;
        }

        if (parentDefinition.Children.Allowed.Count == 0)
        {
            if (childDefinition.Name.Equals("label", StringComparison.OrdinalIgnoreCase) &&
                parentDefinition.Name.Equals("button", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            result.Errors.Add(
                $"Node '{DisplayNodeType(parent.ElementName, parentDefinition)}' does not allow child node '{DisplayNodeType(child.ElementName, childDefinition)}'.");
            return;
        }

        var childNames = GetNodeIdentityNames(child.ElementName, childDefinition);
        var isAllowed = parentDefinition.Children.Allowed.Any(allowed =>
            childNames.Contains(allowed, StringComparer.OrdinalIgnoreCase));

        if (!isAllowed)
        {
            result.Errors.Add(
                $"Node '{DisplayNodeType(parent.ElementName, parentDefinition)}' does not allow child node '{DisplayNodeType(child.ElementName, childDefinition)}'.");
        }
    }

    private static bool IsAllowedValue(
        AttributeValueTypeDefinition typeDefinition,
        IReadOnlyList<string> allowedValues,
        string value)
    {
        if (allowedValues.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        if (typeDefinition.Kind == AttributeType.Color)
        {
            return IsValidColorValue(value);
        }

        return false;
    }

    private static bool IsValidValue(AttributeValueTypeDefinition typeDefinition, string value)
    {
        return typeDefinition.Kind switch
        {
            AttributeType.Boolean => bool.TryParse(value, out _),
            AttributeType.Integer => int.TryParse(value, out _),
            AttributeType.Color => IsValidColorValue(value),
            AttributeType.Size => IsValidSizeValue(value),
            AttributeType.Coordinate => IsValidCoordinateValue(value),
            AttributeType.Event => !string.IsNullOrWhiteSpace(value),
            _ => true
        };
    }

    private static bool IsValidSizeValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.EndsWith('%'))
        {
            return int.TryParse(value[..^1], out _);
        }

        return string.Equals(value, "content", StringComparison.OrdinalIgnoreCase) ||
               int.TryParse(value, out _);
    }

    private static bool IsValidCoordinateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.EndsWith('%'))
        {
            return int.TryParse(value[..^1], out _);
        }

        return int.TryParse(value, out _);
    }

    private static bool IsValidColorValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return HasHexDigits(value.AsSpan(2));
        }

        if (value.StartsWith('#'))
        {
            return HasHexDigits(value.AsSpan(1));
        }

        return false;
    }

    private static bool HasHexDigits(ReadOnlySpan<char> span)
    {
        if (span.Length is not 6 and not 8)
        {
            return false;
        }

        foreach (var ch in span)
        {
            var isHex =
                (ch >= '0' && ch <= '9') ||
                (ch >= 'a' && ch <= 'f') ||
                (ch >= 'A' && ch <= 'F');

            if (!isHex)
            {
                return false;
            }
        }

        return true;
    }

    private LvglElementDefinition? ResolveElement(string elementName)
    {
        if (_registry.TryGet(elementName, out var definition) && definition is not null)
        {
            return definition;
        }

        if (_registry.TryGetByLvglType(elementName, out definition) && definition is not null)
        {
            return definition;
        }

        return null;
    }

    private static string DisplayNodeType(string elementName, LvglElementDefinition definition)
        => string.Equals(elementName, definition.Name, StringComparison.OrdinalIgnoreCase)
            ? definition.Name
            : $"{definition.Name} ({elementName})";

    private static bool IsEventAttribute(string providedAttribute, LvglElementAttributeDefinition definition) =>
        providedAttribute.StartsWith("on_", StringComparison.OrdinalIgnoreCase) ||
        definition.LvglName.StartsWith("on_", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(definition.TypeName, "event", StringComparison.OrdinalIgnoreCase);

    private static bool IsSupportedEvent(LvglElementDefinition definition, string providedAttribute)
    {
        var trigger = providedAttribute.StartsWith("on_", StringComparison.OrdinalIgnoreCase)
            ? providedAttribute[3..]
            : providedAttribute;

        var normalizedTrigger = NormalizeEventName(trigger);

        return definition.Events.Any(x =>
            string.Equals(NormalizeEventName(x.Name), normalizedTrigger, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeEventName(string name) =>
        new string(name.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    private static IReadOnlyList<string> GetNodeIdentityNames(string elementName, LvglElementDefinition definition)
    {
        var names = new List<string>
        {
            elementName,
            definition.Name
        };

        if (definition.Targets.TryGetValue("lvgl", out var target) && !string.IsNullOrWhiteSpace(target.Type))
        {
            names.Add(target.Type);
        }

        return names;
    }

    private static void ValidateRtosMessagesContract(UiDocument document, ValidationResult result)
    {
        var usedIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ValidateRtosMessagesNode(document.Root, result, usedIds);
    }

    private static void ValidateRtosMessagesNode(UiNode node, ValidationResult result, IDictionary<string, string> usedIds)
    {
        var nodeLabel = node.Attributes.TryGetValue("id", out var idValue) && !string.IsNullOrWhiteSpace(idValue)
            ? idValue!
            : node.ElementName;

        if (!string.IsNullOrWhiteSpace(idValue))
        {
            if (usedIds.TryGetValue(idValue!, out var existingNode))
            {
                result.Errors.Add($"RTOS-Messages requires unique ids. Duplicate id '{idValue}' found on '{nodeLabel}' and '{existingNode}'.");
            }
            else
            {
                usedIds[idValue!] = nodeLabel;
            }
        }

        foreach (var evt in node.Events)
        {
            var hasCallback = evt.Attributes.TryGetValue("callback", out var callbackValue) && !string.IsNullOrWhiteSpace(callbackValue);
            if (!hasCallback)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(idValue))
            {
                result.Errors.Add($"RTOS-Messages requires an id on event source '{node.ElementName}' for event '{evt.Name}'.");
            }

            if (!evt.Attributes.TryGetValue("action", out var actionValue) || string.IsNullOrWhiteSpace(actionValue))
            {
                result.Errors.Add($"RTOS-Messages requires an action on event '{evt.Name}' for '{nodeLabel}'.");
            }
        }

        foreach (var child in node.Children)
        {
            ValidateRtosMessagesNode(child, result, usedIds);
        }
    }
}
