using System.Text;
using System.Text.RegularExpressions;
using Ai.McuUiStudio.Core.MetaModel;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

public sealed class LvglCGenerator
{
    private readonly LvglMetaModelRegistry _metaModelRegistry;
    private int _generatedIdentifierIndex;

    public LvglCGenerator()
        : this(LvglMetaModelRegistry.CreateDefault())
    {
    }

    public LvglCGenerator(LvglMetaModelRegistry metaModelRegistry)
    {
        _metaModelRegistry = metaModelRegistry;
    }

    public LvglCGenerationResult Generate(UiDocument document, string unitName = "ui_main")
    {
        _generatedIdentifierIndex = 0;

        var screenDefinition = ResolveElementDefinition(document.Root.ElementName)
            ?? throw new InvalidOperationException($"Unknown root node type '{document.Root.ElementName}'.");

        if (!string.Equals(screenDefinition.Name, "screen", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The LVGL C generator currently expects a screen as the document root.");
        }

        var safeUnitName = SanitizeIdentifier(unitName, "ui_main");
        var rootVariableName = CreateIdentifier(document.Root, screenDefinition, "screen");
        var createFunctionName = $"ui_create_{SanitizeIdentifier(document.Root.Attributes.GetValueOrDefault("name"), "main_screen")}";
        var loadFunctionName = $"ui_load_{SanitizeIdentifier(document.Root.Attributes.GetValueOrDefault("name"), "main_screen")}";
        var headerFileName = $"{safeUnitName}.h";
        var sourceFileName = $"{safeUnitName}.c";
        var exportedHandles = CollectExportedHandles(document.Root).ToList();

        var header = new StringBuilder();
        header.AppendLine("#pragma once");
        header.AppendLine();
        header.AppendLine("#include \"lvgl.h\"");
        header.AppendLine();
        foreach (var handle in exportedHandles)
        {
            header.AppendLine($"extern lv_obj_t *{handle};");
        }

        if (exportedHandles.Count > 0)
        {
            header.AppendLine();
        }

        header.AppendLine($"lv_obj_t *{createFunctionName}(void);");
        header.AppendLine($"void {loadFunctionName}(void);");

        var source = new StringBuilder();
        source.AppendLine($"#include \"{headerFileName}\"");
        source.AppendLine();
        foreach (var handle in exportedHandles)
        {
            source.AppendLine($"lv_obj_t *{handle} = NULL;");
        }

        if (exportedHandles.Count > 0)
        {
            source.AppendLine();
        }

        source.AppendLine($"lv_obj_t *{createFunctionName}(void)");
        source.AppendLine("{");
        foreach (var handle in exportedHandles)
        {
            source.AppendLine($"    {handle} = NULL;");
        }

        if (exportedHandles.Count > 0)
        {
            source.AppendLine();
        }

        source.AppendLine($"    lv_obj_t *{rootVariableName} = lv_obj_create(NULL);");
        AppendNodeAttributes(source, document.Root, screenDefinition, rootVariableName, isScreenRoot: true);

        foreach (var child in document.Root.Children)
        {
            AppendNode(source, child, rootVariableName, 1);
        }

        source.AppendLine();
        source.AppendLine($"    return {rootVariableName};");
        source.AppendLine("}");
        source.AppendLine();
        source.AppendLine($"void {loadFunctionName}(void)");
        source.AppendLine("{");
        source.AppendLine($"    lv_obj_t *screen = {createFunctionName}();");
        source.AppendLine("    lv_screen_load(screen);");
        source.AppendLine("}");

        return new LvglCGenerationResult(headerFileName, sourceFileName, header.ToString(), source.ToString());
    }

    private void AppendNode(StringBuilder source, UiNode node, string parentVariableName, int indentLevel)
    {
        var definition = ResolveElementDefinition(node.ElementName);
        if (definition is null)
        {
            AppendComment(source, indentLevel, $"TODO: unsupported node type '{node.ElementName}'.");
            return;
        }

        if (string.Equals(definition.Name, "scaleSection", StringComparison.OrdinalIgnoreCase))
        {
            var sectionVariableName = CreateIdentifier(node, definition, definition.Name);
            source.AppendLine($"{Indent(indentLevel)}lv_scale_section_t *{sectionVariableName} = lv_scale_add_section({parentVariableName});");
            AppendNodeAttributes(source, node, definition, sectionVariableName, isScreenRoot: false, indentLevel, parentVariableName);
            return;
        }

        var createFunction = ResolveCreateFunction(definition);
        if (string.IsNullOrWhiteSpace(createFunction))
        {
            AppendComment(source, indentLevel, $"TODO: node '{definition.Name}' is not yet supported by the C generator.");
            return;
        }

        var variableName = CreateIdentifier(node, definition, definition.Name);
        var localVariableName = HasExportedHandle(node) ? $"{variableName}_obj" : variableName;
        source.AppendLine($"{Indent(indentLevel)}lv_obj_t *{localVariableName} = {createFunction}({parentVariableName});");
        if (HasExportedHandle(node))
        {
            source.AppendLine($"{Indent(indentLevel)}{variableName} = {localVariableName};");
        }
        AppendNodeAttributes(source, node, definition, localVariableName, isScreenRoot: false, indentLevel, parentVariableName);

        foreach (var child in node.Children)
        {
            AppendNode(source, child, localVariableName, indentLevel);
        }
    }

    private void AppendNodeAttributes(
        StringBuilder source,
        UiNode node,
        LvglElementDefinition definition,
        string variableName,
        bool isScreenRoot,
        int indentLevel = 1,
        string? ownerVariableName = null)
    {
        var emittedSizeSetter = false;
        var emittedGridDescriptorSetter = false;
        var emittedGridCellSetter = false;
        var emittedFlexAlignSetter = false;
        var emittedFlexLayoutSetter = false;
        var emittedSliderRangeSetter = false;
        var emittedSliderStartValueSetter = false;
        var emittedSliderValueSetter = false;
        var emittedBarRangeSetter = false;
        var emittedBarStartValueSetter = false;
        var emittedBarValueSetter = false;
        var emittedLinePointsSetter = false;

        foreach (var attribute in node.Attributes
                     .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                     .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
        {
            var attributeDefinition = ResolveAttributeDefinition(definition, attribute.Key);
            if (attributeDefinition is not null && attributeDefinition.Target == AttributeTarget.CodeTemplate)
            {
                continue;
            }

            var storageName = attributeDefinition?.Name ?? attribute.Key;
            var lvglName = attributeDefinition?.LvglName ?? attribute.Key;
            var value = attribute.Value!;

            if (isScreenRoot && string.Equals(storageName, "name", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            switch (storageName)
            {
                case "width":
                case "height":
                    if (!emittedSizeSetter)
                    {
                        AppendSizeSetter(source, indentLevel, variableName, node.Attributes);
                        emittedSizeSetter = true;
                    }
                    break;
                case "x":
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_x({variableName}, {ToNumericExpression(value)});");
                    break;
                case "y":
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_y({variableName}, {ToNumericExpression(value)});");
                    break;
                case "align":
                    if (!string.Equals(value, "EMPTY", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_obj_set_align({variableName}, {ToEnumExpression("LV_ALIGN_", value)});");
                    }
                    break;
                case "minValue":
                case "maxValue":
                    if (string.Equals(definition.Name, "arc", StringComparison.OrdinalIgnoreCase))
                    {
                        AppendArcRangeSetter(source, indentLevel, variableName, node.Attributes);
                    }
                    else if (string.Equals(definition.Name, "scale", StringComparison.OrdinalIgnoreCase))
                    {
                        AppendScaleRangeSetter(source, indentLevel, variableName, node.Attributes);
                    }
                    else if (string.Equals(definition.Name, "scaleSection", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(ownerVariableName))
                    {
                        AppendScaleSectionRangeSetter(source, indentLevel, ownerVariableName, variableName, node.Attributes);
                    }
                    else if (string.Equals(definition.Name, "spinbox", StringComparison.OrdinalIgnoreCase))
                    {
                        AppendSpinboxRangeSetter(source, indentLevel, variableName, node.Attributes);
                    }
                    else if (string.Equals(definition.Name, "slider", StringComparison.OrdinalIgnoreCase) && !emittedSliderRangeSetter)
                    {
                        AppendSliderRangeSetter(source, indentLevel, variableName, node.Attributes);
                        emittedSliderRangeSetter = true;
                    }
                    else if (string.Equals(definition.Name, "bar", StringComparison.OrdinalIgnoreCase) && !emittedBarRangeSetter)
                    {
                        AppendBarRangeSetter(source, indentLevel, variableName, node.Attributes);
                        emittedBarRangeSetter = true;
                    }
                    break;
                case "startValue":
                    if (string.Equals(definition.Name, "slider", StringComparison.OrdinalIgnoreCase) && !emittedSliderValueSetter)
                    {
                        if (!emittedSliderStartValueSetter)
                        {
                            AppendSliderStartValueSetter(source, indentLevel, variableName, node.Attributes);
                            emittedSliderStartValueSetter = true;
                        }
                    }
                    else if (string.Equals(definition.Name, "bar", StringComparison.OrdinalIgnoreCase) && !emittedBarStartValueSetter)
                    {
                        AppendBarStartValueSetter(source, indentLevel, variableName, node.Attributes);
                        emittedBarStartValueSetter = true;
                    }
                    break;
                case "value":
                    if (string.Equals(definition.Name, "arc", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_arc_set_value({variableName}, {ToNumericExpression(value)});");
                    }
                    else if (string.Equals(definition.Name, "spinbox", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_spinbox_set_value({variableName}, {ToNumericExpression(value)});");
                    }
                    else if (string.Equals(definition.Name, "slider", StringComparison.OrdinalIgnoreCase) && !emittedSliderValueSetter)
                    {
                        AppendSliderValueSetter(source, indentLevel, variableName, node.Attributes);
                        emittedSliderValueSetter = true;
                    }
                    else if (string.Equals(definition.Name, "bar", StringComparison.OrdinalIgnoreCase) && !emittedBarValueSetter)
                    {
                        AppendBarValueSetter(source, indentLevel, variableName, node.Attributes);
                        emittedBarValueSetter = true;
                    }
                    break;
                case "startValueAnim":
                case "startValueAnimated":
                    if (string.Equals(definition.Name, "slider", StringComparison.OrdinalIgnoreCase) && !emittedSliderStartValueSetter)
                    {
                        AppendSliderStartValueSetter(source, indentLevel, variableName, node.Attributes);
                        emittedSliderStartValueSetter = true;
                    }
                    else if (string.Equals(definition.Name, "bar", StringComparison.OrdinalIgnoreCase) && !emittedBarStartValueSetter)
                    {
                        AppendBarStartValueSetter(source, indentLevel, variableName, node.Attributes);
                        emittedBarStartValueSetter = true;
                    }
                    break;
                case "valueAnim":
                case "valueAnimated":
                    if (string.Equals(definition.Name, "slider", StringComparison.OrdinalIgnoreCase) && !emittedSliderValueSetter)
                    {
                        AppendSliderValueSetter(source, indentLevel, variableName, node.Attributes);
                        emittedSliderValueSetter = true;
                    }
                    else if (string.Equals(definition.Name, "bar", StringComparison.OrdinalIgnoreCase) && !emittedBarValueSetter)
                    {
                        AppendBarValueSetter(source, indentLevel, variableName, node.Attributes);
                        emittedBarValueSetter = true;
                    }
                    break;
                case "mode":
                    if (string.Equals(definition.Name, "arc", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_arc_set_mode({variableName}, {ToEnumExpression("LV_ARC_MODE_", value)});");
                    }
                    else if (string.Equals(definition.Name, "scale", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_scale_set_mode({variableName}, {ToEnumExpression("LV_SCALE_MODE_", value)});");
                    }
                    else if (string.Equals(definition.Name, "slider", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_slider_set_mode({variableName}, {ToEnumExpression("LV_SLIDER_MODE_", value)});");
                    }
                    else if (string.Equals(definition.Name, "bar", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_bar_set_mode({variableName}, {ToEnumExpression("LV_BAR_MODE_", value)});");
                    }
                    break;
                case "orientation":
                    if (string.Equals(definition.Name, "slider", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_slider_set_orientation({variableName}, {ToEnumExpression("LV_SLIDER_ORIENTATION_", value)});");
                    }
                    else if (string.Equals(definition.Name, "bar", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_bar_set_orientation({variableName}, {ToEnumExpression("LV_BAR_ORIENTATION_", value)});");
                    }
                    else if (string.Equals(definition.Name, "switch", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_switch_set_orientation({variableName}, {ToEnumExpression("LV_SWITCH_ORIENTATION_", value)});");
                    }
                    break;
                case "points":
                    if (string.Equals(definition.Name, "line", StringComparison.OrdinalIgnoreCase) && !emittedLinePointsSetter)
                    {
                        AppendLinePointsSetter(source, indentLevel, variableName, node.Attributes);
                        emittedLinePointsSetter = true;
                    }
                    break;
                case "yInvert":
                    if (string.Equals(definition.Name, "line", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_line_set_y_invert({variableName}, {ToBoolLiteral(value)});");
                    }
                    break;
                case "checked":
                    AppendBooleanStateSetter(source, indentLevel, variableName, value, "LV_STATE_CHECKED");
                    break;
                case "hidden":
                    AppendBooleanFlagSetter(source, indentLevel, variableName, value, "LV_OBJ_FLAG_HIDDEN");
                    break;
                case "clickable":
                    AppendBooleanFlagSetter(source, indentLevel, variableName, value, "LV_OBJ_FLAG_CLICKABLE");
                    break;
                case "scrollable":
                    AppendBooleanFlagSetter(source, indentLevel, variableName, value, "LV_OBJ_FLAG_SCROLLABLE");
                    break;
                case "disabled":
                    AppendBooleanStateSetter(source, indentLevel, variableName, value, "LV_STATE_DISABLED");
                    break;
                case "layout":
                    AppendLayoutSetter(source, indentLevel, variableName, value);
                    break;
                case "gridColumnDscArray":
                case "gridRowDscArray":
                    if (!emittedGridDescriptorSetter && IsGridLayout(node.Attributes))
                    {
                        AppendGridDescriptorSetter(source, indentLevel, variableName, node.Attributes);
                        emittedGridDescriptorSetter = true;
                    }
                    break;
                case "gridCellColumnPos":
                case "gridCellColumnSpan":
                case "gridCellXAlign":
                case "gridCellRowPos":
                case "gridCellRowSpan":
                case "gridCellYAlign":
                    if (!emittedGridCellSetter)
                    {
                        AppendGridCellSetter(source, indentLevel, variableName, node.Attributes);
                        emittedGridCellSetter = true;
                    }
                    break;
                case "flexFlow":
                    if (!emittedFlexLayoutSetter)
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_obj_set_layout({variableName}, LV_LAYOUT_FLEX);");
                        emittedFlexLayoutSetter = true;
                    }
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_flex_flow({variableName}, {ToEnumExpression("LV_FLEX_FLOW_", value)});");
                    break;
                case "flexMainPlacement":
                case "flexCrossPlacement":
                case "flexTrackPlacement":
                    if (!emittedFlexLayoutSetter)
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_obj_set_layout({variableName}, LV_LAYOUT_FLEX);");
                        emittedFlexLayoutSetter = true;
                    }
                    if (!emittedFlexAlignSetter)
                    {
                        AppendFlexAlignSetter(source, indentLevel, variableName, node.Attributes);
                        emittedFlexAlignSetter = true;
                    }
                    break;
                case "backgroundColor":
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_bg_color({variableName}, {ToColorExpression(value)}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    break;
                case "backgroundOpacity":
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_bg_opa({variableName}, {ToNumericExpression(value)}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    break;
                case "opa":
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_opa({variableName}, {ToNumericExpression(value)}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    break;
                case "padding":
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_pad_all({variableName}, {ToNumericExpression(value)}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    break;
                case "rowSpacing":
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_pad_row({variableName}, {ToNumericExpression(value)}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    break;
                case "columnSpacing":
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_pad_column({variableName}, {ToNumericExpression(value)}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    break;
                case "borderWidth":
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_border_width({variableName}, {ToNumericExpression(value)}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    break;
                case "borderColor":
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_border_color({variableName}, {ToColorExpression(value)}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    break;
                case "radius":
                    if (string.Equals(definition.Name, "arcLabel", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_arclabel_set_radius({variableName}, {ToNumericExpression(value)});");
                    }
                    else
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_radius({variableName}, {ToNumericExpression(value)}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    }
                    break;
                case "text":
                    if (string.Equals(definition.Name, "arcLabel", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_arclabel_set_text({variableName}, {ToCString(value)});");
                    }
                    else if (string.Equals(definition.Name, "checkbox", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_checkbox_set_text({variableName}, {ToCString(value)});");
                    }
                    else if (string.Equals(definition.Name, "dropdown", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_dropdown_set_text({variableName}, {ToCString(value)});");
                    }
                    else if (string.Equals(definition.Name, "textArea", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_textarea_set_text({variableName}, {ToCString(value)});");
                    }
                    else
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_label_set_text({variableName}, {ToCString(value)});");
                    }
                    break;
                case "color":
                    if (string.Equals(definition.Name, "led", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_led_set_color({variableName}, {ToColorExpression(value)});");
                    }
                    else
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_text_color({variableName}, {ToColorExpression(value)}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    }
                    break;
                case "brightness":
                    source.AppendLine($"{Indent(indentLevel)}lv_led_set_brightness({variableName}, {ToNumericExpression(value)});");
                    break;
                case "font":
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_text_font({variableName}, &{value.Trim().TrimStart('&')}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    break;
                case "textAlign":
                    if (string.Equals(definition.Name, "textArea", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_textarea_set_align({variableName}, {ToEnumExpression("LV_TEXT_ALIGN_", value)});");
                    }
                    else
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_text_align({variableName}, {ToEnumExpression("LV_TEXT_ALIGN_", value)}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    }
                    break;
                case "baseDir":
                    source.AppendLine($"{Indent(indentLevel)}lv_obj_set_style_base_dir({variableName}, {ToEnumExpression("LV_BASE_DIR_", value)}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                    break;
                case "longMode":
                    source.AppendLine($"{Indent(indentLevel)}lv_label_set_long_mode({variableName}, {ToEnumExpression("LV_LABEL_LONG_MODE_", value)});");
                    break;
                case "source":
                    if (string.Equals(definition.Name, "animatedImage", StringComparison.OrdinalIgnoreCase))
                    {
                        AppendAnimImageSourceSetter(source, indentLevel, variableName, value);
                    }
                    else
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_image_set_src({variableName}, {ToImageSourceExpression(value)});");
                    }
                    break;
                case "rotation":
                    if (string.Equals(definition.Name, "arc", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_arc_set_rotation({variableName}, {ToNumericExpression(value)});");
                    }
                    else if (string.Equals(definition.Name, "scale", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_scale_set_rotation({variableName}, {ToNumericExpression(value)});");
                    }
                    else
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_image_set_rotation({variableName}, {ToNumericExpression(value)});");
                    }
                    break;
                case "angleRange":
                    if (string.Equals(definition.Name, "scale", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_scale_set_angle_range({variableName}, {ToNumericExpression(value)});");
                    }
                    break;
                case "startAngle":
                    if (string.Equals(definition.Name, "arcLabel", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_arclabel_set_angle_start({variableName}, {ToNumericExpression(value)});");
                    }
                    else if (string.Equals(definition.Name, "arc", StringComparison.OrdinalIgnoreCase))
                    {
                        AppendArcAnglesSetter(source, indentLevel, variableName, node.Attributes);
                    }
                    break;
                case "endAngle":
                    if (string.Equals(definition.Name, "arc", StringComparison.OrdinalIgnoreCase))
                    {
                        AppendArcAnglesSetter(source, indentLevel, variableName, node.Attributes);
                    }
                    break;
                case "bgStartAngle":
                case "bgEndAngle":
                    if (string.Equals(definition.Name, "arc", StringComparison.OrdinalIgnoreCase))
                    {
                        AppendArcBgAnglesSetter(source, indentLevel, variableName, node.Attributes);
                    }
                    break;
                case "options":
                    if (string.Equals(definition.Name, "dropdown", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_dropdown_set_options({variableName}, {ToCString(value)});");
                    }
                    else if (string.Equals(definition.Name, "roller", StringComparison.OrdinalIgnoreCase))
                    {
                        var mode = GetAttributeValue(node.Attributes, "optionsMode", "options_mode") ?? "normal";
                        source.AppendLine($"{Indent(indentLevel)}lv_roller_set_options({variableName}, {ToCString(value)}, {ToEnumExpression("LV_ROLLER_MODE_", mode)});");
                    }
                    break;
                case "selected":
                    if (string.Equals(definition.Name, "dropdown", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_dropdown_set_selected({variableName}, {ToNumericExpression(value)});");
                    }
                    else if (string.Equals(definition.Name, "roller", StringComparison.OrdinalIgnoreCase))
                    {
                        var anim = ToAnimExpression(GetAttributeValue(node.Attributes, "selectedAnimated", "selected_animated"));
                        source.AppendLine($"{Indent(indentLevel)}lv_roller_set_selected({variableName}, {ToNumericExpression(value)}, {anim});");
                    }
                    break;
                case "selectedHighlight":
                    source.AppendLine($"{Indent(indentLevel)}lv_dropdown_set_selected_highlight({variableName}, {ToBoolLiteral(value)});");
                    break;
                case "direction":
                    source.AppendLine($"{Indent(indentLevel)}lv_dropdown_set_dir({variableName}, {ToEnumExpression("LV_DIR_", value)});");
                    break;
                case "symbol":
                    source.AppendLine($"{Indent(indentLevel)}lv_dropdown_set_symbol({variableName}, {ToCString(value)});");
                    break;
                case "visibleRowCount":
                    source.AppendLine($"{Indent(indentLevel)}lv_roller_set_visible_row_count({variableName}, {ToNumericExpression(value)});");
                    break;
                case "totalTickCount":
                    source.AppendLine($"{Indent(indentLevel)}lv_scale_set_total_tick_count({variableName}, {ToNumericExpression(value)});");
                    break;
                case "majorTickEvery":
                    source.AppendLine($"{Indent(indentLevel)}lv_scale_set_major_tick_every({variableName}, {ToNumericExpression(value)});");
                    break;
                case "labelShow":
                    source.AppendLine($"{Indent(indentLevel)}lv_scale_set_label_show({variableName}, {ToBoolLiteral(value)});");
                    break;
                case "postDraw":
                    source.AppendLine($"{Indent(indentLevel)}lv_scale_set_post_draw({variableName}, {ToBoolLiteral(value)});");
                    break;
                case "drawTicksOnTop":
                    source.AppendLine($"{Indent(indentLevel)}lv_scale_set_draw_ticks_on_top({variableName}, {ToBoolLiteral(value)});");
                    break;
                case "digitCount":
                case "decPointPos":
                    if (string.Equals(definition.Name, "spinbox", StringComparison.OrdinalIgnoreCase))
                    {
                        AppendSpinboxDigitFormatSetter(source, indentLevel, variableName, node.Attributes);
                    }
                    break;
                case "step":
                    source.AppendLine($"{Indent(indentLevel)}lv_spinbox_set_step({variableName}, {ToNumericExpression(value)});");
                    break;
                case "rollover":
                    source.AppendLine($"{Indent(indentLevel)}lv_spinbox_set_rollover({variableName}, {ToBoolLiteral(value)});");
                    break;
                case "placeholderText":
                    source.AppendLine($"{Indent(indentLevel)}lv_textarea_set_placeholder_text({variableName}, {ToCString(value)});");
                    break;
                case "cursorPos":
                    if (string.Equals(definition.Name, "textArea", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_textarea_set_cursor_pos({variableName}, {ToNumericExpression(value)});");
                    }
                    else if (string.Equals(definition.Name, "spinbox", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_spinbox_set_cursor_pos({variableName}, {ToNumericExpression(value)});");
                    }
                    break;
                case "oneLine":
                    source.AppendLine($"{Indent(indentLevel)}lv_textarea_set_one_line({variableName}, {ToBoolLiteral(value)});");
                    break;
                case "passwordMode":
                    source.AppendLine($"{Indent(indentLevel)}lv_textarea_set_password_mode({variableName}, {ToBoolLiteral(value)});");
                    break;
                case "passwordShowTime":
                    source.AppendLine($"{Indent(indentLevel)}lv_textarea_set_password_show_time({variableName}, {ToNumericExpression(value)});");
                    break;
                case "textSelection":
                    source.AppendLine($"{Indent(indentLevel)}lv_textarea_set_text_selection({variableName}, {ToBoolLiteral(value)});");
                    break;
                case "animationTime":
                case "arcLength":
                    if (string.Equals(definition.Name, "spinner", StringComparison.OrdinalIgnoreCase))
                    {
                        AppendSpinnerAnimSetter(source, indentLevel, variableName, node.Attributes);
                    }
                    break;
                case "data":
                    source.AppendLine($"{Indent(indentLevel)}lv_qrcode_set_data({variableName}, {ToCString(value)});");
                    break;
                case "size":
                    if (string.Equals(definition.Name, "qrCode", StringComparison.OrdinalIgnoreCase))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_qrcode_set_size({variableName}, {ToNumericExpression(value)});");
                    }
                    else
                    {
                        AppendComment(source, indentLevel, $"TODO: unsupported property '{storageName}' (lvgl '{lvglName}') = '{value}'.");
                    }
                    break;
                case "darkColor":
                    source.AppendLine($"{Indent(indentLevel)}lv_qrcode_set_dark_color({variableName}, {ToColorExpression(value)});");
                    break;
                case "lightColor":
                    source.AppendLine($"{Indent(indentLevel)}lv_qrcode_set_light_color({variableName}, {ToColorExpression(value)});");
                    break;
                case "quietZone":
                    source.AppendLine($"{Indent(indentLevel)}lv_qrcode_set_quiet_zone({variableName}, {ToBoolLiteral(value)});");
                    break;
                case "pivotX":
                case "pivotY":
                    if (!string.IsNullOrWhiteSpace(GetAttributeValue(node.Attributes, "pivotX", "pivot_x")) &&
                        !string.IsNullOrWhiteSpace(GetAttributeValue(node.Attributes, "pivotY", "pivot_y")))
                    {
                        source.AppendLine($"{Indent(indentLevel)}lv_image_set_pivot({variableName}, {ToNumericExpression(GetAttributeValue(node.Attributes, "pivotX", "pivot_x")!)}, {ToNumericExpression(GetAttributeValue(node.Attributes, "pivotY", "pivot_y")!)});");
                    }
                    break;
                case "scaleX":
                    source.AppendLine($"{Indent(indentLevel)}lv_image_set_scale_x({variableName}, {ToNumericExpression(value)});");
                    break;
                case "scaleY":
                    source.AppendLine($"{Indent(indentLevel)}lv_image_set_scale_y({variableName}, {ToNumericExpression(value)});");
                    break;
                case "id":
                    break;
                default:
                    AppendComment(source, indentLevel, $"TODO: unsupported property '{storageName}' (lvgl '{lvglName}') = '{value}'.");
                    break;
            }
        }
    }

    private static void AppendSizeSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        if (!attributes.TryGetValue("width", out var width) && !attributes.TryGetValue("Width", out width))
        {
            width = null;
        }

        if (!attributes.TryGetValue("height", out var height) && !attributes.TryGetValue("Height", out height))
        {
            height = null;
        }

        if (string.IsNullOrWhiteSpace(width) || string.IsNullOrWhiteSpace(height))
        {
            if (!string.IsNullOrWhiteSpace(width))
            {
                source.AppendLine($"{Indent(indentLevel)}lv_obj_set_width({variableName}, {ToSizeExpression(width)});");
            }

            if (!string.IsNullOrWhiteSpace(height))
            {
                source.AppendLine($"{Indent(indentLevel)}lv_obj_set_height({variableName}, {ToSizeExpression(height)});");
            }

            return;
        }

        source.AppendLine($"{Indent(indentLevel)}lv_obj_set_size({variableName}, {ToSizeExpression(width)}, {ToSizeExpression(height)});");
    }

    private static void AppendLayoutSetter(StringBuilder source, int indentLevel, string variableName, string value)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "grid":
                source.AppendLine($"{Indent(indentLevel)}lv_obj_set_layout({variableName}, LV_LAYOUT_GRID);");
                break;
            case "row":
                source.AppendLine($"{Indent(indentLevel)}lv_obj_set_layout({variableName}, LV_LAYOUT_FLEX);");
                source.AppendLine($"{Indent(indentLevel)}lv_obj_set_flex_flow({variableName}, LV_FLEX_FLOW_ROW);");
                break;
            case "column":
                source.AppendLine($"{Indent(indentLevel)}lv_obj_set_layout({variableName}, LV_LAYOUT_FLEX);");
                source.AppendLine($"{Indent(indentLevel)}lv_obj_set_flex_flow({variableName}, LV_FLEX_FLOW_COLUMN);");
                break;
            case "none":
                source.AppendLine($"{Indent(indentLevel)}lv_obj_set_layout({variableName}, LV_LAYOUT_NONE);");
                break;
            case "empty":
                break;
            default:
                source.AppendLine($"{Indent(indentLevel)}/* TODO: unsupported layout '{value}'. */");
                break;
        }
    }

    private static void AppendSliderRangeSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var minValue = GetAttributeValue(attributes, "minValue", "min_value") ?? "0";
        var maxValue = GetAttributeValue(attributes, "maxValue", "max_value") ?? "100";
        source.AppendLine($"{Indent(indentLevel)}lv_slider_set_range({variableName}, {ToNumericExpression(minValue)}, {ToNumericExpression(maxValue)});");
    }

    private static void AppendSliderValueSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var value = GetAttributeValue(attributes, "value", "value");

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var anim = ToAnimExpression(GetAttributeValue(attributes, "valueAnim", "value_anim"));
        source.AppendLine($"{Indent(indentLevel)}lv_slider_set_value({variableName}, {ToNumericExpression(value)}, {anim});");
    }

    private static void AppendSliderStartValueSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var value = GetAttributeValue(attributes, "startValue", "start_value");

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var anim = ToAnimExpression(GetAttributeValue(attributes, "startValueAnim", "start_value_anim"));
        source.AppendLine($"{Indent(indentLevel)}lv_slider_set_start_value({variableName}, {ToNumericExpression(value)}, {anim});");
    }

    private static void AppendBarRangeSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var minValue = GetAttributeValue(attributes, "minValue", "min_value") ?? "0";
        var maxValue = GetAttributeValue(attributes, "maxValue", "max_value") ?? "100";
        source.AppendLine($"{Indent(indentLevel)}lv_bar_set_range({variableName}, {ToNumericExpression(minValue)}, {ToNumericExpression(maxValue)});");
    }

    private static void AppendBarValueSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var value = GetAttributeValue(attributes, "value", "value");

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var anim = ToAnimExpression(GetAttributeValue(attributes, "valueAnimated", "value_animated"));
        source.AppendLine($"{Indent(indentLevel)}lv_bar_set_value({variableName}, {ToNumericExpression(value)}, {anim});");
    }

    private static void AppendBarStartValueSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var value = GetAttributeValue(attributes, "startValue", "start_value");

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var anim = ToAnimExpression(GetAttributeValue(attributes, "startValueAnimated", "start_value_animated"));
        source.AppendLine($"{Indent(indentLevel)}lv_bar_set_start_value({variableName}, {ToNumericExpression(value)}, {anim});");
    }

    private static void AppendArcRangeSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var minValue = GetAttributeValue(attributes, "minValue", "min_value") ?? "0";
        var maxValue = GetAttributeValue(attributes, "maxValue", "max_value") ?? "100";
        source.AppendLine($"{Indent(indentLevel)}lv_arc_set_range({variableName}, {ToNumericExpression(minValue)}, {ToNumericExpression(maxValue)});");
    }

    private static void AppendScaleRangeSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var minValue = GetAttributeValue(attributes, "minValue", "min_value") ?? "0";
        var maxValue = GetAttributeValue(attributes, "maxValue", "max_value") ?? "100";
        source.AppendLine($"{Indent(indentLevel)}lv_scale_set_range({variableName}, {ToNumericExpression(minValue)}, {ToNumericExpression(maxValue)});");
    }

    private static void AppendScaleSectionRangeSetter(StringBuilder source, int indentLevel, string ownerVariableName, string sectionVariableName, IDictionary<string, string?> attributes)
    {
        var minValue = GetAttributeValue(attributes, "minValue", "min_value");
        var maxValue = GetAttributeValue(attributes, "maxValue", "max_value");

        if (!string.IsNullOrWhiteSpace(minValue))
        {
            source.AppendLine($"{Indent(indentLevel)}lv_scale_set_section_min_value({ownerVariableName}, {sectionVariableName}, {ToNumericExpression(minValue)});");
        }

        if (!string.IsNullOrWhiteSpace(maxValue))
        {
            source.AppendLine($"{Indent(indentLevel)}lv_scale_set_section_max_value({ownerVariableName}, {sectionVariableName}, {ToNumericExpression(maxValue)});");
        }
    }

    private static void AppendArcAnglesSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var start = GetAttributeValue(attributes, "startAngle", "start_angle");
        var end = GetAttributeValue(attributes, "endAngle", "end_angle");
        if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
        {
            return;
        }

        source.AppendLine($"{Indent(indentLevel)}lv_arc_set_angles({variableName}, {ToNumericExpression(start)}, {ToNumericExpression(end)});");
    }

    private static void AppendArcBgAnglesSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var start = GetAttributeValue(attributes, "bgStartAngle", "bg_start_angle");
        var end = GetAttributeValue(attributes, "bgEndAngle", "bg_end_angle");
        if (string.IsNullOrWhiteSpace(start) || string.IsNullOrWhiteSpace(end))
        {
            return;
        }

        source.AppendLine($"{Indent(indentLevel)}lv_arc_set_bg_angles({variableName}, {ToNumericExpression(start)}, {ToNumericExpression(end)});");
    }

    private static void AppendSpinboxRangeSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var minValue = GetAttributeValue(attributes, "minValue", "min_value") ?? "0";
        var maxValue = GetAttributeValue(attributes, "maxValue", "max_value") ?? "100";
        source.AppendLine($"{Indent(indentLevel)}lv_spinbox_set_range({variableName}, {ToNumericExpression(minValue)}, {ToNumericExpression(maxValue)});");
    }

    private static void AppendSpinboxDigitFormatSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var digitCount = GetAttributeValue(attributes, "digitCount", "digit_count");
        var decPointPos = GetAttributeValue(attributes, "decPointPos", "dec_point_pos");
        if (string.IsNullOrWhiteSpace(digitCount) || string.IsNullOrWhiteSpace(decPointPos))
        {
            return;
        }

        source.AppendLine($"{Indent(indentLevel)}lv_spinbox_set_digit_format({variableName}, {ToNumericExpression(digitCount)}, {ToNumericExpression(decPointPos)});");
    }

    private static void AppendSpinnerAnimSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var time = GetAttributeValue(attributes, "animationTime", "anim_time");
        var angle = GetAttributeValue(attributes, "arcLength", "arc_length");
        if (string.IsNullOrWhiteSpace(time) || string.IsNullOrWhiteSpace(angle))
        {
            return;
        }

        source.AppendLine($"{Indent(indentLevel)}lv_spinner_set_anim_params({variableName}, {ToNumericExpression(time)}, {ToNumericExpression(angle)});");
    }

    private static void AppendAnimImageSourceSetter(StringBuilder source, int indentLevel, string variableName, string value)
    {
        var frames = value
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ToImageSourceExpression)
            .ToList();

        if (frames.Count == 0)
        {
            return;
        }

        source.AppendLine($"{Indent(indentLevel)}static const void * {variableName}_frames[] = {{ {string.Join(", ", frames)} }};");
        source.AppendLine($"{Indent(indentLevel)}lv_animimg_set_src({variableName}, {variableName}_frames, {frames.Count});");
    }

    private static void AppendLinePointsSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var rawPoints = GetAttributeValue(attributes, "points", "linePoints");
        var points = ParseLinePoints(rawPoints);
        if (points.Count == 0)
        {
            return;
        }

        var minX = points.Min(static point => point.X);
        var minY = points.Min(static point => point.Y);
        var normalizedPoints = points
            .Select(static point => point)
            .Select(point => (X: point.X - minX, Y: point.Y - minY))
            .ToList();

        source.AppendLine($"{Indent(indentLevel)}static lv_point_precise_t {variableName}_points[] = {{ {string.Join(", ", normalizedPoints.Select(static point => $"{{ {point.X}, {point.Y} }}"))} }};");
        source.AppendLine($"{Indent(indentLevel)}lv_line_set_points({variableName}, {variableName}_points, {normalizedPoints.Count});");

        if (GetAttributeValue(attributes, "x", "X") is null)
        {
            source.AppendLine($"{Indent(indentLevel)}lv_obj_set_x({variableName}, {minX});");
        }

        if (GetAttributeValue(attributes, "y", "Y") is null)
        {
            source.AppendLine($"{Indent(indentLevel)}lv_obj_set_y({variableName}, {minY});");
        }
    }

    private static void AppendGridDescriptorSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var columnTemplate = GetAttributeValue(attributes, "gridColumnDscArray", "grid_column_dsc_array");
        var rowTemplate = GetAttributeValue(attributes, "gridRowDscArray", "grid_row_dsc_array");

        if (string.IsNullOrWhiteSpace(columnTemplate) && string.IsNullOrWhiteSpace(rowTemplate))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(columnTemplate))
        {
            source.AppendLine($"{Indent(indentLevel)}static int32_t {variableName}_col_dsc[] = {{ {ToGridTemplateExpression(columnTemplate)} }};");
        }

        if (!string.IsNullOrWhiteSpace(rowTemplate))
        {
            source.AppendLine($"{Indent(indentLevel)}static int32_t {variableName}_row_dsc[] = {{ {ToGridTemplateExpression(rowTemplate)} }};");
        }

        source.AppendLine(
            $"{Indent(indentLevel)}lv_obj_set_grid_dsc_array({variableName}, " +
            $"{(!string.IsNullOrWhiteSpace(columnTemplate) ? $"{variableName}_col_dsc" : "NULL")}, " +
            $"{(!string.IsNullOrWhiteSpace(rowTemplate) ? $"{variableName}_row_dsc" : "NULL")});");
    }

    private static void AppendGridCellSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var columnAlign = ToGridAlignExpression(GetAttributeValue(attributes, "gridCellXAlign", "grid_cell_x_align") ?? "stretch");
        var columnPos = ToNumericExpression(GetAttributeValue(attributes, "gridCellColumnPos", "grid_cell_column_pos") ?? "0");
        var columnSpan = ToNumericExpression(GetAttributeValue(attributes, "gridCellColumnSpan", "grid_cell_column_span") ?? "1");
        var rowAlign = ToGridAlignExpression(GetAttributeValue(attributes, "gridCellYAlign", "grid_cell_y_align") ?? "stretch");
        var rowPos = ToNumericExpression(GetAttributeValue(attributes, "gridCellRowPos", "grid_cell_row_pos") ?? "0");
        var rowSpan = ToNumericExpression(GetAttributeValue(attributes, "gridCellRowSpan", "grid_cell_row_span") ?? "1");

        source.AppendLine(
            $"{Indent(indentLevel)}lv_obj_set_grid_cell({variableName}, {columnAlign}, {columnPos}, {columnSpan}, {rowAlign}, {rowPos}, {rowSpan});");
    }

    private static void AppendFlexAlignSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var mainAlign = ToFlexAlignExpression(GetAttributeValue(attributes, "flexMainPlacement", "flex_main_place") ?? "start");
        var crossAlign = ToFlexAlignExpression(GetAttributeValue(attributes, "flexCrossPlacement", "flex_cross_place") ?? "start");
        var trackAlign = ToFlexAlignExpression(GetAttributeValue(attributes, "flexTrackPlacement", "flex_track_place") ?? "start");

        source.AppendLine(
            $"{Indent(indentLevel)}lv_obj_set_flex_align({variableName}, {mainAlign}, {crossAlign}, {trackAlign});");
    }

    private static bool IsGridLayout(IDictionary<string, string?> attributes)
    {
        var layout = GetAttributeValue(attributes, "layout", "Layout");
        return string.Equals(layout?.Trim(), "grid", StringComparison.OrdinalIgnoreCase);
    }

    private LvglElementDefinition? ResolveElementDefinition(string typeName)
    {
        if (_metaModelRegistry.TryGet(typeName, out var definition) && definition is not null)
        {
            return definition;
        }

        if (_metaModelRegistry.TryGetByLvglType(typeName, out var lvglDefinition) && lvglDefinition is not null)
        {
            return lvglDefinition;
        }

        return null;
    }

    private static LvglElementAttributeDefinition? ResolveAttributeDefinition(LvglElementDefinition definition, string attributeName) =>
        definition.Attributes.FirstOrDefault(x =>
            string.Equals(x.Name, attributeName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.LvglName, attributeName, StringComparison.OrdinalIgnoreCase));

    private static string? ResolveCreateFunction(LvglElementDefinition definition) =>
        definition.Name switch
        {
            "view" => "lv_obj_create",
            "texture3d" => "lv_3dtexture_create",
            "animatedImage" => "lv_animimg_create",
            "arc" => "lv_arc_create",
            "arcLabel" => "lv_arclabel_create",
            "bar" => "lv_bar_create",
            "button" => "lv_button_create",
            "buttonMatrix" => "lv_buttonmatrix_create",
            "canvas" => "lv_canvas_create",
            "checkbox" => "lv_checkbox_create",
            "dropdown" => "lv_dropdown_create",
            "label" => "lv_label_create",
            "image" => "lv_image_create",
            "imageButton" => "lv_imagebutton_create",
            "led" => "lv_led_create",
            "line" => "lv_line_create",
            "lottie" => "lv_lottie_create",
            "qrCode" => "lv_qrcode_create",
            "roller" => "lv_roller_create",
            "scale" => "lv_scale_create",
            "slider" => "lv_slider_create",
            "spinbox" => "lv_spinbox_create",
            "spinner" => "lv_spinner_create",
            "switch" => "lv_switch_create",
            "textArea" => "lv_textarea_create",
            _ => null
        };

    private static void AppendBooleanStateSetter(StringBuilder source, int indentLevel, string variableName, string value, string stateConstant)
    {
        if (ToBoolean(value))
        {
            source.AppendLine($"{Indent(indentLevel)}lv_obj_add_state({variableName}, {stateConstant});");
        }
        else
        {
            source.AppendLine($"{Indent(indentLevel)}lv_obj_remove_state({variableName}, {stateConstant});");
        }
    }

    private static void AppendBooleanFlagSetter(StringBuilder source, int indentLevel, string variableName, string value, string flagConstant)
    {
        if (ToBoolean(value))
        {
            source.AppendLine($"{Indent(indentLevel)}lv_obj_add_flag({variableName}, {flagConstant});");
        }
        else
        {
            source.AppendLine($"{Indent(indentLevel)}lv_obj_remove_flag({variableName}, {flagConstant});");
        }
    }

    private string CreateIdentifier(UiNode node, LvglElementDefinition definition, string fallbackBase)
    {
        var preferredValue = node.Attributes.GetValueOrDefault("id")
                             ?? node.Attributes.GetValueOrDefault("name")
                             ?? definition.Name
                             ?? fallbackBase;
        var identifier = SanitizeIdentifier(preferredValue, fallbackBase);
        if (identifier == fallbackBase)
        {
            identifier = $"{identifier}_{++_generatedIdentifierIndex}";
        }

        return identifier;
    }

    private IEnumerable<string> CollectExportedHandles(UiNode root)
    {
        foreach (var node in EnumerateNodes(root))
        {
            if (HasExportedHandle(node))
            {
                yield return SanitizeIdentifier(node.Attributes["id"], "obj");
            }
        }
    }

    private static IEnumerable<UiNode> EnumerateNodes(UiNode root)
    {
        foreach (var child in root.Children)
        {
            yield return child;

            foreach (var descendant in EnumerateNodes(child))
            {
                yield return descendant;
            }
        }
    }

    private static bool HasExportedHandle(UiNode node) =>
        node.Attributes.TryGetValue("id", out var idValue) && !string.IsNullOrWhiteSpace(idValue);

    private static string ToSizeExpression(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.EndsWith('%') && int.TryParse(trimmed[..^1], out var percentage))
        {
            return $"LV_PCT({percentage})";
        }

        return ToNumericExpression(trimmed);
    }

    private static string ToNumericExpression(string value) =>
        int.TryParse(value.Trim(), out var integerValue) ? integerValue.ToString() : value.Trim();

    private static bool ToBoolean(string value)
    {
        var trimmed = value.Trim();
        return string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(trimmed, "1", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(trimmed, "yes", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(trimmed, "on", StringComparison.OrdinalIgnoreCase);
    }

    private static string ToBoolLiteral(string value) => ToBoolean(value) ? "true" : "false";

    private static string ToAnimExpression(string? value) => value is not null && ToBoolean(value) ? "LV_ANIM_ON" : "LV_ANIM_OFF";

    private static string ToColorExpression(string value)
    {
        var trimmed = value.Trim();
        return trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? $"lv_color_hex({trimmed})"
            : trimmed;
    }

    private static string ToGridTemplateExpression(string value)
    {
        var items = value
            .Split([' ', '\t', '\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ToSingleGridTrackExpression)
            .ToList();
        items.Add("LV_GRID_TEMPLATE_LAST");
        return string.Join(", ", items);
    }

    private static string ToSingleGridTrackExpression(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("fr(", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(')'))
        {
            var inner = trimmed[3..^1];
            return $"LV_GRID_FR({ToNumericExpression(inner)})";
        }

        if (string.Equals(trimmed, "content", StringComparison.OrdinalIgnoreCase))
        {
            return "LV_GRID_CONTENT";
        }

        return ToNumericExpression(trimmed);
    }

    private static string ToEnumExpression(string prefix, string value)
    {
        var normalizedValue = value
            .Trim()
            .Replace("-", "_", StringComparison.OrdinalIgnoreCase)
            .Replace(" ", "_", StringComparison.OrdinalIgnoreCase)
            .ToUpperInvariant();
        return $"{prefix}{normalizedValue}";
    }

    private static string ToGridAlignExpression(string value) => ToEnumExpression("LV_GRID_ALIGN_", value);

    private static string ToFlexAlignExpression(string value) => ToEnumExpression("LV_FLEX_ALIGN_", value);

    private static string ToCString(string value) =>
        $"\"{value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal)}\"";

    private static string ToImageSourceExpression(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith('&') || trimmed.StartsWith("LV_", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        return ToCString(trimmed);
    }

    private static void AppendComment(StringBuilder source, int indentLevel, string text) =>
        source.AppendLine($"{Indent(indentLevel)}/* {text} */");

    private static string Indent(int level) => new(' ', level * 4);

    private static string SanitizeIdentifier(string? rawValue, string fallback)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return fallback;
        }

        var normalized = Regex.Replace(rawValue.Trim(), @"[^A-Za-z0-9_]+", "_");
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return fallback;
        }

        if (char.IsDigit(normalized[0]))
        {
            normalized = $"_{normalized}";
        }

        return normalized;
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

    private static List<(int X, int Y)> ParseLinePoints(string? rawPoints)
    {
        if (string.IsNullOrWhiteSpace(rawPoints))
        {
            return [];
        }

        var matches = Regex.Matches(rawPoints, @"\(\s*(-?\d+)\s*,\s*(-?\d+)\s*\)");
        var points = new List<(int X, int Y)>(matches.Count);
        foreach (Match match in matches)
        {
            if (!match.Success)
            {
                continue;
            }

            if (int.TryParse(match.Groups[1].Value, out var x) &&
                int.TryParse(match.Groups[2].Value, out var y))
            {
                points.Add((x, y));
            }
        }

        return points;
    }
}
