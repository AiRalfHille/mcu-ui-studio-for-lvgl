using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Ai.McuUiStudio.Core.MetaModel;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

public sealed class McuDisplayCodeGenerator
{
    private readonly LvglMetaModelRegistry _metaModelRegistry;
    private int _generatedIdentifierIndex;
    private string _currentUnitName = "ui_start";
    private HashSet<string>? _explicitExportIds;

    public McuDisplayCodeGenerator()
        : this(LvglMetaModelRegistry.CreateDefault())
    {
    }

    public McuDisplayCodeGenerator(LvglMetaModelRegistry metaModelRegistry)
    {
        _metaModelRegistry = metaModelRegistry;
    }

    public McuDisplayCodeGenerationResult Generate(UiDocument document, string unitName = "ui_start")
        => Generate(document, unitName, null);

    public McuDisplayCodeGenerationResult Generate(UiDocument document, string unitName, IReadOnlyList<string>? exportedObjectIds)
    {
        _generatedIdentifierIndex = 0;
        _currentUnitName = SanitizeIdentifier(unitName, "ui_start");
        _explicitExportIds = exportedObjectIds is null
            ? null
            : new HashSet<string>(exportedObjectIds.Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.OrdinalIgnoreCase);

        var screenDefinition = ResolveElementDefinition(document.Root.ElementName)
            ?? throw new InvalidOperationException($"Unknown root node type '{document.Root.ElementName}'.");

        if (!string.Equals(screenDefinition.Name, "screen", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The MCU display code generator currently expects a screen as the document root.");
        }

        var safeUnitName = _currentUnitName;
        var headerFileName = $"{safeUnitName}.h";
        var sourceFileName = $"{safeUnitName}.c";
        var rootVariableName = "screen";
        var objectHandles = (_explicitExportIds is { Count: > 0 }
                ? _explicitExportIds.Select(id => new
                    {
                        ObjectName = id,
                        HandleName = SanitizeIdentifier(id, "obj")
                    })
                : CollectExportedIds(document.Root)
                    .Select(id => new
                    {
                        ObjectName = id,
                        HandleName = SanitizeIdentifier(id, "obj")
                    }))
            .DistinctBy(target => target.HandleName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(target => target.ObjectName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var header = new StringBuilder();
        header.AppendLine("#pragma once");
        header.AppendLine();
        if (objectHandles.Length > 0)
        {
            header.AppendLine("#include \"lvgl.h\"");
            header.AppendLine();
            foreach (var handle in objectHandles)
            {
                header.AppendLine($"extern lv_obj_t * {handle.HandleName};");
            }
            header.AppendLine();
        }
        header.AppendLine($"void {safeUnitName}_init(void);");

        var source = new StringBuilder();
        source.AppendLine($"#include \"{headerFileName}\"");
        source.AppendLine("#include \"lvgl.h\"");
        source.AppendLine();
        source.AppendLine("/* weitere Includes nach Bedarf */");
        source.AppendLine();
        foreach (var handle in objectHandles)
        {
            source.AppendLine($"lv_obj_t * {handle.HandleName} = NULL;");
        }

        if (objectHandles.Length > 0)
        {
            source.AppendLine();
        }

        source.AppendLine("static void create_layout(void)");
        source.AppendLine("{");
        foreach (var handle in objectHandles)
        {
            source.AppendLine($"    {handle.HandleName} = NULL;");
        }

        if (objectHandles.Length > 0)
        {
            source.AppendLine();
        }

        source.AppendLine($"    lv_obj_t *{rootVariableName} = lv_obj_create(NULL);");
        AppendUpdateHandleAssignment(source, document.Root, rootVariableName, 1);
        AppendNodeAttributes(source, document.Root, screenDefinition, rootVariableName, isScreenRoot: true);

        foreach (var child in document.Root.Children)
        {
            AppendNode(source, child, rootVariableName, 1);
        }

        source.AppendLine();
        source.AppendLine("    lv_screen_load(screen);");
        source.AppendLine("}");
        source.AppendLine();
        source.AppendLine($"void {safeUnitName}_init(void)");
        source.AppendLine("{");
        source.AppendLine("    create_layout();");
        source.AppendLine("}");

        _explicitExportIds = null;

        return new McuDisplayCodeGenerationResult(headerFileName, sourceFileName, header.ToString(), source.ToString());
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
            AppendComment(source, indentLevel, $"TODO: node '{definition.Name}' is not yet supported by the MCU display generator.");
            return;
        }

        var variableName = CreateIdentifier(node, definition, definition.Name);
        var localVariableName = HasExportedHandle(node) ? $"{variableName}_obj" : variableName;
        source.AppendLine($"{Indent(indentLevel)}lv_obj_t *{localVariableName} = {createFunction}({parentVariableName});");
        AppendUpdateHandleAssignment(source, node, localVariableName, indentLevel);
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
        var emittedImagePivotSetter = false;

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
                case "id":
                    break;
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
                        source.AppendLine($"{Indent(indentLevel)}lv_roller_set_selected({variableName}, {ToNumericExpression(value)}, {ToAnimExpression(GetAttributeValue(node.Attributes, "selectedAnimated", "selected_animated"))});");
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
                case "pivotX":
                case "pivotY":
                    if (!emittedImagePivotSetter)
                    {
                        var pivotX = GetAttributeValue(node.Attributes, "pivotX", "pivot_x");
                        var pivotY = GetAttributeValue(node.Attributes, "pivotY", "pivot_y");
                        if (!string.IsNullOrWhiteSpace(pivotX) && !string.IsNullOrWhiteSpace(pivotY))
                        {
                            source.AppendLine($"{Indent(indentLevel)}lv_image_set_pivot({variableName}, {ToNumericExpression(pivotX)}, {ToNumericExpression(pivotY)});");
                            emittedImagePivotSetter = true;
                        }
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
                case "scaleX":
                    source.AppendLine($"{Indent(indentLevel)}lv_image_set_scale_x({variableName}, {ToNumericExpression(value)});");
                    break;
                case "scaleY":
                    source.AppendLine($"{Indent(indentLevel)}lv_image_set_scale_y({variableName}, {ToNumericExpression(value)});");
                    break;
                default:
                    AppendComment(source, indentLevel, $"TODO: unsupported property '{storageName}' (lvgl '{lvglName}') = '{value}'.");
                    break;
            }
        }
    }

    private LvglElementDefinition? ResolveElementDefinition(string typeName)
    {
        if (_metaModelRegistry.TryGet(typeName, out var directDefinition) && directDefinition is not null)
        {
            return directDefinition;
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

    private static string? ResolveCreateFunction(LvglElementDefinition definition)
    {
        var typeName = definition.Targets.TryGetValue("lvgl", out var targetDefinition)
            ? targetDefinition.Type
            : definition.Name;

        return typeName switch
        {
            "screen" => "lv_obj_create",
            "lv_obj" => "lv_obj_create",
            "lv_3dtexture" => "lv_3dtexture_create",
            "lv_animimg" => "lv_animimg_create",
            "lv_arc" => "lv_arc_create",
            "lv_arclabel" => "lv_arclabel_create",
            "lv_bar" => "lv_bar_create",
            "lv_button" => "lv_button_create",
            "lv_buttonmatrix" => "lv_buttonmatrix_create",
            "lv_calendar" => "lv_calendar_create",
            "lv_canvas" => "lv_canvas_create",
            "lv_chart" => "lv_chart_create",
            "lv_checkbox" => "lv_checkbox_create",
            "lv_dropdown" => "lv_dropdown_create",
            "lv_image" => "lv_image_create",
            "lv_imagebutton" => "lv_imagebutton_create",
            "lv_keyboard" => "lv_keyboard_create",
            "lv_label" => "lv_label_create",
            "lv_led" => "lv_led_create",
            "lv_line" => "lv_line_create",
            "lv_list" => "lv_list_create",
            "lv_lottie" => "lv_lottie_create",
            "lv_menu" => "lv_menu_create",
            "lv_msgbox" => "lv_msgbox_create",
            "lv_qrcode" => "lv_qrcode_create",
            "lv_roller" => "lv_roller_create",
            "lv_scale" => "lv_scale_create",
            "lv_slider" => "lv_slider_create",
            "lv_spangroup" => "lv_spangroup_create",
            "lv_spinbox" => "lv_spinbox_create",
            "lv_spinner" => "lv_spinner_create",
            "lv_switch" => "lv_switch_create",
            "lv_table" => "lv_table_create",
            "lv_tabview" => "lv_tabview_create",
            "lv_textarea" => "lv_textarea_create",
            "lv_tileview" => "lv_tileview_create",
            "lv_win" => "lv_win_create",
            _ => null
        };
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

        if (string.IsNullOrWhiteSpace(width) && string.IsNullOrWhiteSpace(height))
        {
            return;
        }

        var widthExpression = ToNumericExpression(width ?? "LV_SIZE_CONTENT");
        var heightExpression = ToNumericExpression(height ?? "LV_SIZE_CONTENT");
        source.AppendLine($"{Indent(indentLevel)}lv_obj_set_size({variableName}, {widthExpression}, {heightExpression});");
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
        source.AppendLine($"{Indent(indentLevel)}lv_obj_set_grid_cell({variableName}, {columnAlign}, {columnPos}, {columnSpan}, {rowAlign}, {rowPos}, {rowSpan});");
    }

    private static void AppendFlexAlignSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var mainAlign = ToFlexAlignExpression(GetAttributeValue(attributes, "flexMainPlacement", "flex_main_place") ?? "start");
        var crossAlign = ToFlexAlignExpression(GetAttributeValue(attributes, "flexCrossPlacement", "flex_cross_place") ?? "start");
        var trackAlign = ToFlexAlignExpression(GetAttributeValue(attributes, "flexTrackPlacement", "flex_track_place") ?? "start");
        source.AppendLine($"{Indent(indentLevel)}lv_obj_set_flex_align({variableName}, {mainAlign}, {crossAlign}, {trackAlign});");
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

        source.AppendLine($"{Indent(indentLevel)}lv_slider_set_value({variableName}, {ToNumericExpression(value)}, {ToAnimExpression(GetAttributeValue(attributes, "valueAnim", "value_anim"))});");
    }

    private static void AppendSliderStartValueSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var value = GetAttributeValue(attributes, "startValue", "start_value");
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        source.AppendLine($"{Indent(indentLevel)}lv_slider_set_start_value({variableName}, {ToNumericExpression(value)}, {ToAnimExpression(GetAttributeValue(attributes, "startValueAnim", "start_value_anim"))});");
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

        source.AppendLine($"{Indent(indentLevel)}lv_bar_set_value({variableName}, {ToNumericExpression(value)}, {ToAnimExpression(GetAttributeValue(attributes, "valueAnimated", "value_animated"))});");
    }

    private static void AppendBarStartValueSetter(StringBuilder source, int indentLevel, string variableName, IDictionary<string, string?> attributes)
    {
        var value = GetAttributeValue(attributes, "startValue", "start_value");
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        source.AppendLine($"{Indent(indentLevel)}lv_bar_set_start_value({variableName}, {ToNumericExpression(value)}, {ToAnimExpression(GetAttributeValue(attributes, "startValueAnimated", "start_value_animated"))});");
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
        var normalizedPoints = points.Select(point => (X: point.X - minX, Y: point.Y - minY)).ToList();

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

    private static bool IsGridLayout(IDictionary<string, string?> attributes)
    {
        var layout = GetAttributeValue(attributes, "layout", "Layout");
        return string.Equals(layout, "grid", StringComparison.OrdinalIgnoreCase);
    }

    private static void AppendLayoutSetter(StringBuilder source, int indentLevel, string variableName, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "EMPTY", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case "row":
            case "column":
                source.AppendLine($"{Indent(indentLevel)}lv_obj_set_layout({variableName}, LV_LAYOUT_FLEX);");
                source.AppendLine($"{Indent(indentLevel)}lv_obj_set_flex_flow({variableName}, {ToEnumExpression("LV_FLEX_FLOW_", value)});");
                break;
            case "grid":
                source.AppendLine($"{Indent(indentLevel)}lv_obj_set_layout({variableName}, LV_LAYOUT_GRID);");
                break;
            case "none":
                source.AppendLine($"{Indent(indentLevel)}lv_obj_set_layout({variableName}, 0);");
                break;
            default:
                source.AppendLine($"{Indent(indentLevel)}/* TODO: unsupported layout '{value}'. */");
                break;
        }
    }

    private static void AppendBooleanFlagSetter(StringBuilder source, int indentLevel, string variableName, string value, string flagExpression)
    {
        if (TryParseBoolean(value, out var boolValue))
        {
            source.AppendLine(boolValue
                ? $"{Indent(indentLevel)}lv_obj_add_flag({variableName}, {flagExpression});"
                : $"{Indent(indentLevel)}lv_obj_remove_flag({variableName}, {flagExpression});");
        }
    }

    private static void AppendBooleanStateSetter(StringBuilder source, int indentLevel, string variableName, string value, string stateExpression)
    {
        if (TryParseBoolean(value, out var boolValue))
        {
            source.AppendLine(boolValue
                ? $"{Indent(indentLevel)}lv_obj_add_state({variableName}, {stateExpression});"
                : $"{Indent(indentLevel)}lv_obj_remove_state({variableName}, {stateExpression});");
        }
    }

    private static bool TryParseBoolean(string value, out bool result)
    {
        if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1")
        {
            result = true;
            return true;
        }

        if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) || value == "0")
        {
            result = false;
            return true;
        }

        result = false;
        return false;
    }

    private string CreateIdentifier(UiNode node, LvglElementDefinition definition, string fallbackPrefix)
    {
        var candidate = node.Attributes.GetValueOrDefault("id")
                        ?? node.Attributes.GetValueOrDefault("name")
                        ?? $"{fallbackPrefix}_{++_generatedIdentifierIndex}";

        if (_explicitExportIds is { Count: > 0 } &&
            node.Attributes.TryGetValue("id", out var idValue) &&
            !string.IsNullOrWhiteSpace(idValue) &&
            _explicitExportIds.Contains(idValue))
        {
            candidate = $"{candidate}_obj";
        }

        return SanitizeIdentifier(candidate, fallbackPrefix);
    }

    private void AppendUpdateHandleAssignment(
        StringBuilder source,
        UiNode node,
        string variableName,
        int indentLevel)
    {
        if (_explicitExportIds is { Count: > 0 })
        {
            var explicitObjectName = GetAttributeValue(node.Attributes, "id");
            if (string.IsNullOrWhiteSpace(explicitObjectName) || !_explicitExportIds.Contains(explicitObjectName))
            {
                return;
            }

            var explicitHandleName = SanitizeIdentifier(explicitObjectName, "obj");
            source.AppendLine($"{Indent(indentLevel)}{explicitHandleName} = {variableName};");
            return;
        }

        var objectName = GetAttributeValue(node.Attributes, "id");
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return;
        }

        var handleName = SanitizeIdentifier(objectName, "obj");
        source.AppendLine($"{Indent(indentLevel)}{handleName} = {variableName};");
    }

    private static bool HasExportedHandle(UiNode node)
    {
        var id = GetAttributeValue(node.Attributes, "id");
        return !string.IsNullOrWhiteSpace(id);
    }

    private static IEnumerable<string> CollectExportedIds(UiNode root)
    {
        foreach (var child in root.Children)
        {
            foreach (var id in CollectExportedIdsRecursive(child))
            {
                yield return id;
            }
        }
    }

    private static IEnumerable<string> CollectExportedIdsRecursive(UiNode node)
    {
        var id = GetAttributeValue(node.Attributes, "id");
        if (!string.IsNullOrWhiteSpace(id))
        {
            yield return id;
        }

        foreach (var child in node.Children)
        {
            foreach (var nestedId in CollectExportedIdsRecursive(child))
            {
                yield return nestedId;
            }
        }
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

    private static string ToNumericExpression(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.EndsWith('%'))
        {
            var numeric = trimmed[..^1].Trim();
            return $"lv_pct({numeric})";
        }

        return trimmed;
    }

    private static string ToEnumExpression(string prefix, string value) =>
        prefix + value.Trim().Replace('-', '_').Replace(' ', '_').ToUpperInvariant();

    private static string ToBoolLiteral(string value) => TryParseBoolean(value, out var result) && result ? "true" : "false";

    private static string ToAnimExpression(string? value) => value is not null && TryParseBoolean(value, out var result) && result ? "LV_ANIM_ON" : "LV_ANIM_OFF";

    private static string ToGridAlignExpression(string value) => ToEnumExpression("LV_GRID_ALIGN_", value);

    private static string ToFlexAlignExpression(string value) => ToEnumExpression("LV_FLEX_ALIGN_", value);

    private static string ToColorExpression(string value) => $"lv_color_hex({value.Trim()})";

    private static string ToGridTemplateExpression(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "NULL";
        }

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

    private static string ToImageSourceExpression(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("&", StringComparison.Ordinal))
        {
            return trimmed;
        }

        return ToCString(trimmed);
    }

    private static string ToCString(string value) =>
        "\"" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal) + "\"";

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

    private static void AppendComment(StringBuilder source, int indentLevel, string text)
    {
        source.AppendLine($"{Indent(indentLevel)}/* {text} */");
    }

    private static string Indent(int level) => new(' ', level * 4);

    private static List<(int X, int Y)> ParseLinePoints(string? value)
    {
        var result = new List<(int X, int Y)>();
        if (string.IsNullOrWhiteSpace(value))
        {
            return result;
        }

        foreach (Match match in Regex.Matches(value, @"\(\s*(-?\d+)\s*,\s*(-?\d+)\s*\)"))
        {
            if (match.Groups.Count >= 3)
            {
                result.Add((int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)));
            }
        }

        return result;
    }
}
