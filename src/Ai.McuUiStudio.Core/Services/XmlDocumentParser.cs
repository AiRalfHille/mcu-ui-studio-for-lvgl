using System.Xml.Linq;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

public sealed class XmlDocumentParser
{
    public UiDocument Parse(string xml)
    {
        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new InvalidOperationException("XML enthaelt kein Root-Element.");
        return new UiDocument(ParseNode(root));
    }

    private static UiNode ParseNode(XElement element)
    {
        var node = new UiNode(element.Name.LocalName);

        foreach (var attribute in element.Attributes())
        {
            var mapped = MapAttributeName(element.Name.LocalName, attribute.Name.LocalName, attribute.Value);
            if (mapped is null)
            {
                continue;
            }

            node.Attributes[mapped.Value.Name] = mapped.Value.Value;
        }

        foreach (var child in element.Elements())
        {
            if (TryMapGeneratedEventCallback(child, node))
            {
                continue;
            }

            node.Children.Add(ParseNode(child));
        }

        return node;
    }

    private static bool TryMapGeneratedEventCallback(XElement child, UiNode parent)
    {
        if (!string.Equals(child.Name.LocalName, "event_cb", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var callback = child.Attribute("callback")?.Value;
        var trigger = child.Attribute("trigger")?.Value;
        var userData = child.Attribute("user_data")?.Value;

        if (!string.Equals(callback, "sim_log_event", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(trigger) ||
            string.IsNullOrWhiteSpace(userData))
        {
            return false;
        }

        parent.Attributes[$"on_{trigger}"] = userData;
        return true;
    }

    private static (string Name, string Value)? MapAttributeName(string elementName, string attributeName, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (string.Equals(elementName, "screen", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(attributeName, "name", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (string.Equals(attributeName, "name", StringComparison.OrdinalIgnoreCase))
        {
            return ("id", value);
        }

        if (TryMapStyleAttribute(attributeName, value, out var styleMapped))
        {
            return styleMapped;
        }

        return attributeName switch
        {
            "popover" => ("popover", value),
            "map" when string.Equals(elementName, "lv_buttonmatrix", StringComparison.OrdinalIgnoreCase) => ("options", value),
            "value-animated" => ("value_animated", value),
            "start_value-animated" => ("start_value_animated", value),
            "value-anim" => ("value_anim", value),
            "start_value-anim" => ("start_value_anim", value),
            "options-mode" => ("options_mode", value),
            _ => (attributeName, value)
        };
    }

    private static bool TryMapStyleAttribute(string attributeName, string value, out (string Name, string Value) mapped)
    {
        mapped = attributeName switch
        {
            "style_bg_color" => ("bg_color", value),
            "style_bg_opa" => ("bg_opa", value),
            "style_pad_all" => ("pad_all", value),
            "style_pad_row" => ("pad_row", value),
            "style_pad_column" => ("pad_column", value),
            "style_border_width" => ("border_width", value),
            "style_border_color" => ("border_color", value),
            "style_radius" => ("radius", value),
            "style_opa" => ("opa", value),
            "style_grid_column_dsc_array" => ("grid_column_dsc_array", value),
            "style_grid_row_dsc_array" => ("grid_row_dsc_array", value),
            "style_grid_cell_column_pos" => ("grid_cell_column_pos", value),
            "style_grid_cell_column_span" => ("grid_cell_column_span", value),
            "style_grid_cell_x_align" => ("grid_cell_x_align", value),
            "style_grid_cell_row_pos" => ("grid_cell_row_pos", value),
            "style_grid_cell_row_span" => ("grid_cell_row_span", value),
            "style_grid_cell_y_align" => ("grid_cell_y_align", value),
            "style_layout" when string.Equals(value, "grid", StringComparison.OrdinalIgnoreCase) => ("layout", "grid"),
            "style_text_font" => ("font", value),
            "style_text_color" => ("color", value),
            _ => default
        };

        return mapped != default;
    }
}
