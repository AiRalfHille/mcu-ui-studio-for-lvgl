using System.Xml.Linq;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

public sealed class XmlGenerator
{
    public string Generate(UiDocument document)
    {
        var xml = new XDocument(CreateElement(document.Root));
        return xml.ToString();
    }

    private static XElement CreateElement(UiNode node)
    {
        var element = new XElement(node.ElementName);

        foreach (var attribute in node.Attributes
                     .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                     .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            ApplyAttribute(element, node.ElementName, attribute.Key, attribute.Value!);
        }

        foreach (var child in node.Children)
        {
            element.Add(CreateElement(child));
        }

        AddGeneratedEventChildren(element, node);

        return element;
    }

    private static void ApplyAttribute(XElement element, string elementName, string attributeName, string value)
    {
        if (string.Equals(elementName, "screen", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(attributeName, "name", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(attributeName, "id", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("name", value);
            return;
        }

        if (string.Equals(attributeName, "layout", StringComparison.OrdinalIgnoreCase))
        {
            ApplyLayoutAttribute(element, value);
            return;
        }

        if (string.Equals(attributeName, "popovers", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(attributeName, "popover", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("popover", value);
            return;
        }

        if (string.Equals(attributeName, "options", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(elementName, "lv_buttonmatrix", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("map", value);
            return;
        }

        if (string.Equals(attributeName, "value_animated", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("value-animated", value);
            return;
        }

        if (string.Equals(attributeName, "start_value_animated", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("start_value-animated", value);
            return;
        }

        if (string.Equals(attributeName, "value_anim", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("value-anim", value);
            return;
        }

        if (string.Equals(attributeName, "start_value_anim", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("start_value-anim", value);
            return;
        }

        if (string.Equals(attributeName, "showed_year", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("shown_year", value);
            return;
        }

        if (string.Equals(attributeName, "showed_month", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("shown_month", value);
            return;
        }

        if (string.Equals(attributeName, "separator_position", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("dec_point_pos", value);
            return;
        }

        if (string.Equals(attributeName, "options_mode", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("options-mode", value);
            return;
        }

        if (string.Equals(attributeName, "selected_animated", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("selected-animated", value);
            return;
        }

        if (string.Equals(elementName, "lv_roller", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(attributeName, "mode", StringComparison.OrdinalIgnoreCase))
        {
            element.SetAttributeValue("options-mode", value);
            return;
        }

        if (attributeName.StartsWith("on_", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (TryApplyStyleAttribute(element, attributeName, value))
        {
            return;
        }

        element.SetAttributeValue(attributeName, value);
    }

    private static void ApplyLayoutAttribute(XElement element, string value)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "row":
                element.SetAttributeValue("flex_flow", "row");
                break;
            case "column":
                element.SetAttributeValue("flex_flow", "column");
                break;
            case "grid":
                element.SetAttributeValue("style_layout", "grid");
                break;
            case "none":
                break;
            default:
                element.SetAttributeValue("layout", value);
                break;
        }
    }

    private static void AddGeneratedEventChildren(XElement element, UiNode node)
    {
        foreach (var evt in node.Events
                     .Where(x => x.Attributes.TryGetValue("callback", out var callback) && !string.IsNullOrWhiteSpace(callback))
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var triggerName = evt.Name;
            var callback = evt.Attributes["callback"]!;
            element.Add(
                new XElement(
                    "event_cb",
                    new XAttribute("callback", "sim_log_event"),
                    new XAttribute("trigger", triggerName),
                    new XAttribute("user_data", callback)));
        }
    }

    private static bool TryApplyStyleAttribute(XElement element, string attributeName, string value)
    {
        switch (attributeName)
        {
            case "bg_color":
                element.SetAttributeValue("style_bg_color", value);
                return true;
            case "bg_opa":
                element.SetAttributeValue("style_bg_opa", value);
                return true;
            case "pad_all":
                element.SetAttributeValue("style_pad_all", value);
                return true;
            case "pad_row":
                element.SetAttributeValue("style_pad_row", value);
                return true;
            case "pad_column":
                element.SetAttributeValue("style_pad_column", value);
                return true;
            case "grid_column_dsc_array":
                element.SetAttributeValue("style_grid_column_dsc_array", value);
                return true;
            case "grid_row_dsc_array":
                element.SetAttributeValue("style_grid_row_dsc_array", value);
                return true;
            case "grid_cell_column_pos":
                element.SetAttributeValue("style_grid_cell_column_pos", value);
                return true;
            case "grid_cell_column_span":
                element.SetAttributeValue("style_grid_cell_column_span", value);
                return true;
            case "grid_cell_x_align":
                element.SetAttributeValue("style_grid_cell_x_align", value);
                return true;
            case "grid_cell_row_pos":
                element.SetAttributeValue("style_grid_cell_row_pos", value);
                return true;
            case "grid_cell_row_span":
                element.SetAttributeValue("style_grid_cell_row_span", value);
                return true;
            case "grid_cell_y_align":
                element.SetAttributeValue("style_grid_cell_y_align", value);
                return true;
            case "border_width":
                element.SetAttributeValue("style_border_width", value);
                return true;
            case "border_color":
                element.SetAttributeValue("style_border_color", value);
                return true;
            case "radius":
                element.SetAttributeValue("style_radius", value);
                return true;
            case "opa":
                element.SetAttributeValue("style_opa", value);
                return true;
            case "text_align":
                element.SetAttributeValue("style_text_align", value);
                return true;
            case "font":
                element.SetAttributeValue("style_text_font", value);
                return true;
            case "color":
                element.SetAttributeValue("style_text_color", value);
                return true;
            case "base_dir":
                element.SetAttributeValue("style_base_dir", value);
                return true;
            case "flex_main_place":
                element.SetAttributeValue("style_flex_main_place", value);
                return true;
            case "flex_cross_place":
                element.SetAttributeValue("style_flex_cross_place", value);
                return true;
            case "flex_track_place":
                element.SetAttributeValue("style_flex_track_place", value);
                return true;
            default:
                return false;
        }
    }
}
