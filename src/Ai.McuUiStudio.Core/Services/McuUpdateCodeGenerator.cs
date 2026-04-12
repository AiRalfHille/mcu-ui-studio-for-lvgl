using System.Text;
using System.Text.RegularExpressions;
using Ai.McuUiStudio.Core.MetaModel;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

public sealed class McuUpdateCodeGenerator
{
    private readonly LvglMetaModelRegistry _metaModelRegistry;

    public McuUpdateCodeGenerator()
        : this(LvglMetaModelRegistry.CreateDefault())
    {
    }

    public McuUpdateCodeGenerator(LvglMetaModelRegistry metaModelRegistry)
    {
        _metaModelRegistry = metaModelRegistry;
    }

    public McuUpdateCodeGenerationResult Generate(UiDocument document, string unitName = "ui_start")
    {
        var safeUnitName = SanitizeIdentifier(unitName, "ui_start");
        var headerFileName = $"{safeUnitName}_update.h";
        var sourceFileName = $"{safeUnitName}_update.c";
        var targets = McuUpdateModel.CollectTargets(document, _metaModelRegistry);

        return new McuUpdateCodeGenerationResult(
            headerFileName,
            sourceFileName,
            BuildHeader(safeUnitName, targets),
            BuildSource(safeUnitName, headerFileName, targets),
            targets.Count > 0);
    }

    private static string BuildHeader(string unitName, IReadOnlyList<McuUpdateTarget> targets)
    {
        var typePrefix = $"{unitName}_update";
        var builder = new StringBuilder();
        builder.AppendLine("#pragma once");
        builder.AppendLine();
        builder.AppendLine("#include <stdbool.h>");
        builder.AppendLine("#include <stdint.h>");
        builder.AppendLine("#include \"lvgl.h\"");
        builder.AppendLine();
        builder.AppendLine("typedef enum");
        builder.AppendLine("{");

        if (targets.Count == 0)
        {
            builder.AppendLine($"    {unitName.ToUpperInvariant()}_UPDATE_TARGET_NONE = 0");
        }
        else
        {
            var allTargets = targets
                .SelectMany(target => target.Properties.Select(property => McuUpdateModel.CreateTargetEnumName(unitName, target.ObjectName, property)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            for (var index = 0; index < allTargets.Length; index++)
            {
                var suffix = index == allTargets.Length - 1 ? string.Empty : ",";
                builder.AppendLine($"    {allTargets[index]}{suffix}");
            }
        }

        builder.AppendLine($"}} {typePrefix}_target_t;");
        builder.AppendLine();
        builder.AppendLine("typedef enum");
        builder.AppendLine("{");
        builder.AppendLine($"    {unitName.ToUpperInvariant()}_UPDATE_VALUE_TEXT,");
        builder.AppendLine($"    {unitName.ToUpperInvariant()}_UPDATE_VALUE_INT32,");
        builder.AppendLine($"    {unitName.ToUpperInvariant()}_UPDATE_VALUE_BOOL,");
        builder.AppendLine($"    {unitName.ToUpperInvariant()}_UPDATE_VALUE_COLOR,");
        builder.AppendLine($"    {unitName.ToUpperInvariant()}_UPDATE_VALUE_FONT");
        builder.AppendLine($"}} {typePrefix}_value_type_t;");
        builder.AppendLine();
        builder.AppendLine("typedef struct");
        builder.AppendLine("{");
        builder.AppendLine($"    {typePrefix}_value_type_t type;");
        builder.AppendLine("    union");
        builder.AppendLine("    {");
        builder.AppendLine("        const char *text;");
        builder.AppendLine("        int32_t int32_value;");
        builder.AppendLine("        bool bool_value;");
        builder.AppendLine("        lv_color_t color_value;");
        builder.AppendLine("        const lv_font_t *font_value;");
        builder.AppendLine("    } value;");
        builder.AppendLine($"}} {typePrefix}_value_t;");
        builder.AppendLine();
        builder.AppendLine("typedef struct");
        builder.AppendLine("{");
        builder.AppendLine($"    {typePrefix}_target_t target;");
        builder.AppendLine($"    {typePrefix}_value_t payload;");
        builder.AppendLine($"}} {typePrefix}_message_t;");
        builder.AppendLine();
        builder.AppendLine($"void {unitName}_apply_update(const {typePrefix}_message_t * message);");
        builder.AppendLine($"void {unitName}_update_text({typePrefix}_target_t target, const char * value);");
        builder.AppendLine($"void {unitName}_update_value({typePrefix}_target_t target, int32_t value);");
        builder.AppendLine($"void {unitName}_update_checked({typePrefix}_target_t target, bool value);");
        builder.AppendLine($"void {unitName}_update_visible({typePrefix}_target_t target, bool value);");
        builder.AppendLine($"void {unitName}_update_enabled({typePrefix}_target_t target, bool value);");
        builder.AppendLine($"void {unitName}_update_text_color({typePrefix}_target_t target, lv_color_t value);");
        builder.AppendLine($"void {unitName}_update_background_color({typePrefix}_target_t target, lv_color_t value);");
        builder.AppendLine($"void {unitName}_update_font({typePrefix}_target_t target, const lv_font_t * value);");

        if (targets.Count > 0)
        {
            builder.AppendLine();
            foreach (var target in targets.OrderBy(item => item.ObjectName, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var property in target.Properties)
                {
                    builder.AppendLine($"{GetWrapperSignature(unitName, target.ObjectName, property)};");
                }
            }
        }
        else
        {
            builder.AppendLine();
            builder.AppendLine("/* No inbound UI updates configured for this unit. */");
        }

        return builder.ToString();
    }

    private static string BuildSource(string unitName, string headerFileName, IReadOnlyList<McuUpdateTarget> targets)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"#include \"{headerFileName}\"");
        builder.AppendLine($"#include \"{unitName}.h\"");
        builder.AppendLine();

        builder.AppendLine($"void {unitName}_update_text({unitName}_update_target_t target, const char * value)");
        builder.AppendLine("{");
        builder.AppendLine("    if(value == NULL)");
        builder.AppendLine("    {");
        builder.AppendLine("        return;");
        builder.AppendLine("    }");
        builder.AppendLine();
        AppendPropertySwitch(builder, unitName, targets, McuUpdatePropertyKind.Text, "value");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine($"void {unitName}_update_value({unitName}_update_target_t target, int32_t value)");
        builder.AppendLine("{");
        AppendPropertySwitch(builder, unitName, targets, McuUpdatePropertyKind.Value, "value");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine($"void {unitName}_update_checked({unitName}_update_target_t target, bool value)");
        builder.AppendLine("{");
        AppendPropertySwitch(builder, unitName, targets, McuUpdatePropertyKind.Checked, "value");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine($"void {unitName}_update_visible({unitName}_update_target_t target, bool value)");
        builder.AppendLine("{");
        AppendPropertySwitch(builder, unitName, targets, McuUpdatePropertyKind.Visible, "value");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine($"void {unitName}_update_enabled({unitName}_update_target_t target, bool value)");
        builder.AppendLine("{");
        AppendPropertySwitch(builder, unitName, targets, McuUpdatePropertyKind.Enabled, "value");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine($"void {unitName}_update_text_color({unitName}_update_target_t target, lv_color_t value)");
        builder.AppendLine("{");
        AppendPropertySwitch(builder, unitName, targets, McuUpdatePropertyKind.TextColor, "value");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine($"void {unitName}_update_background_color({unitName}_update_target_t target, lv_color_t value)");
        builder.AppendLine("{");
        AppendPropertySwitch(builder, unitName, targets, McuUpdatePropertyKind.BackgroundColor, "value");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine($"void {unitName}_update_font({unitName}_update_target_t target, const lv_font_t * value)");
        builder.AppendLine("{");
        builder.AppendLine("    if(value == NULL)");
        builder.AppendLine("    {");
        builder.AppendLine("        return;");
        builder.AppendLine("    }");
        builder.AppendLine();
        AppendPropertySwitch(builder, unitName, targets, McuUpdatePropertyKind.Font, "value");
        builder.AppendLine("}");
        builder.AppendLine();

        builder.AppendLine($"void {unitName}_apply_update(const {unitName}_update_message_t * message)");
        builder.AppendLine("{");
        builder.AppendLine("    if(message == NULL)");
        builder.AppendLine("    {");
        builder.AppendLine("        return;");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    switch(message->target)");
        builder.AppendLine("    {");
        foreach (var target in targets.OrderBy(item => item.ObjectName, StringComparer.OrdinalIgnoreCase))
        {
            foreach (var property in target.Properties)
            {
                builder.AppendLine($"        case {McuUpdateModel.CreateTargetEnumName(unitName, target.ObjectName, property)}:");
                builder.AppendLine("        {");
                builder.AppendLine($"            if(message->payload.type != {GetExpectedValueTypeEnum(unitName, property)})");
                builder.AppendLine("            {");
                builder.AppendLine("                return;");
                builder.AppendLine("            }");
                builder.AppendLine($"            {GetPropertyApplyCall(unitName, property, "message->target", "message->payload")};");
                builder.AppendLine("            return;");
                builder.AppendLine("        }");
            }
        }
        builder.AppendLine("        default:");
        builder.AppendLine("            return;");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        if (targets.Count > 0)
        {
            builder.AppendLine();
            foreach (var target in targets.OrderBy(item => item.ObjectName, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var property in target.Properties)
                {
                    builder.AppendLine($"{GetWrapperSignature(unitName, target.ObjectName, property)}");
                    builder.AppendLine("{");
                    builder.AppendLine($"    {GetWrapperInvocation(unitName, target.ObjectName, property)};");
                    builder.AppendLine("}");
                    builder.AppendLine();
                }
            }
        }
        else
        {
            builder.AppendLine();
            builder.AppendLine("/* No inbound UI updates configured for this unit. */");
        }

        return builder.ToString();
    }

    private static void AppendPropertySwitch(StringBuilder builder, string unitName, IReadOnlyList<McuUpdateTarget> targets, McuUpdatePropertyKind property, string valueExpression)
    {
        builder.AppendLine("    switch(target)");
        builder.AppendLine("    {");

        var matchingTargets = targets
            .Where(target => target.Properties.Contains(property))
            .OrderBy(target => target.ObjectName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (matchingTargets.Count == 0)
        {
            builder.AppendLine("        default:");
            builder.AppendLine("            return;");
            builder.AppendLine("    }");
            return;
        }

        foreach (var target in matchingTargets)
        {
            var handleName = McuUpdateModel.CreateObjectHandleName(unitName, target.ObjectName);
            builder.AppendLine($"        case {McuUpdateModel.CreateTargetEnumName(unitName, target.ObjectName, property)}:");
            builder.AppendLine("        {");
            builder.AppendLine($"            if({handleName} == NULL)");
            builder.AppendLine("            {");
            builder.AppendLine("                return;");
            builder.AppendLine("            }");
            builder.Append(GeneratePropertyBody(target, property, handleName, valueExpression));
            builder.AppendLine("            return;");
            builder.AppendLine("        }");
        }

        builder.AppendLine("        default:");
        builder.AppendLine("            return;");
        builder.AppendLine("    }");
    }

    private static string GeneratePropertyBody(McuUpdateTarget target, McuUpdatePropertyKind property, string handleName, string valueExpression)
    {
        var builder = new StringBuilder();
        switch (property)
        {
            case McuUpdatePropertyKind.Text:
                AppendTextUpdate(builder, target.DefinitionName, handleName, valueExpression);
                break;
            case McuUpdatePropertyKind.Value:
                AppendValueUpdate(builder, target.DefinitionName, handleName, valueExpression);
                break;
            case McuUpdatePropertyKind.Checked:
                builder.AppendLine($"            if({valueExpression})");
                builder.AppendLine("            {");
                builder.AppendLine($"                lv_obj_add_state({handleName}, LV_STATE_CHECKED);");
                builder.AppendLine("            }");
                builder.AppendLine("            else");
                builder.AppendLine("            {");
                builder.AppendLine($"                lv_obj_remove_state({handleName}, LV_STATE_CHECKED);");
                builder.AppendLine("            }");
                break;
            case McuUpdatePropertyKind.Visible:
                builder.AppendLine($"            if({valueExpression})");
                builder.AppendLine("            {");
                builder.AppendLine($"                lv_obj_remove_flag({handleName}, LV_OBJ_FLAG_HIDDEN);");
                builder.AppendLine("            }");
                builder.AppendLine("            else");
                builder.AppendLine("            {");
                builder.AppendLine($"                lv_obj_add_flag({handleName}, LV_OBJ_FLAG_HIDDEN);");
                builder.AppendLine("            }");
                break;
            case McuUpdatePropertyKind.Enabled:
                builder.AppendLine($"            if({valueExpression})");
                builder.AppendLine("            {");
                builder.AppendLine($"                lv_obj_remove_state({handleName}, LV_STATE_DISABLED);");
                builder.AppendLine("            }");
                builder.AppendLine("            else");
                builder.AppendLine("            {");
                builder.AppendLine($"                lv_obj_add_state({handleName}, LV_STATE_DISABLED);");
                builder.AppendLine("            }");
                break;
            case McuUpdatePropertyKind.TextColor:
                builder.AppendLine($"            lv_obj_set_style_text_color({handleName}, {valueExpression}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                break;
            case McuUpdatePropertyKind.BackgroundColor:
                builder.AppendLine($"            lv_obj_set_style_bg_color({handleName}, {valueExpression}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                break;
            case McuUpdatePropertyKind.Font:
                builder.AppendLine($"            lv_obj_set_style_text_font({handleName}, {valueExpression}, LV_PART_MAIN | LV_STATE_DEFAULT);");
                break;
        }

        return builder.ToString();
    }

    private static void AppendTextUpdate(StringBuilder builder, string definitionName, string handleName, string valueExpression)
    {
        switch (definitionName)
        {
            case "label":
                builder.AppendLine($"            lv_label_set_text({handleName}, {valueExpression});");
                break;
            case "button":
                builder.AppendLine($"            lv_obj_t * label = lv_obj_get_child({handleName}, 0);");
                builder.AppendLine("            if(label == NULL)");
                builder.AppendLine("            {");
                builder.AppendLine("                return;");
                builder.AppendLine("            }");
                builder.AppendLine($"            lv_label_set_text(label, {valueExpression});");
                break;
            case "checkbox":
                builder.AppendLine($"            lv_checkbox_set_text({handleName}, {valueExpression});");
                break;
            case "textarea":
                builder.AppendLine($"            lv_textarea_set_text({handleName}, {valueExpression});");
                break;
        }
    }

    private static void AppendValueUpdate(StringBuilder builder, string definitionName, string handleName, string valueExpression)
    {
        switch (definitionName)
        {
            case "bar":
                builder.AppendLine($"            lv_bar_set_value({handleName}, {valueExpression}, LV_ANIM_OFF);");
                break;
            case "slider":
                builder.AppendLine($"            lv_slider_set_value({handleName}, {valueExpression}, LV_ANIM_OFF);");
                break;
            case "arc":
                builder.AppendLine($"            lv_arc_set_value({handleName}, {valueExpression});");
                break;
            case "spinbox":
                builder.AppendLine($"            lv_spinbox_set_value({handleName}, {valueExpression});");
                break;
        }
    }

    private static string GetWrapperSignature(string unitName, string objectName, McuUpdatePropertyKind property)
    {
        var functionName = McuUpdateModel.CreateWrapperFunctionName(unitName, objectName, property);
        return property switch
        {
            McuUpdatePropertyKind.Text => $"void {functionName}(const char * value)",
            McuUpdatePropertyKind.Value => $"void {functionName}(int32_t value)",
            McuUpdatePropertyKind.Checked => $"void {functionName}(bool value)",
            McuUpdatePropertyKind.Visible => $"void {functionName}(bool value)",
            McuUpdatePropertyKind.Enabled => $"void {functionName}(bool value)",
            McuUpdatePropertyKind.TextColor => $"void {functionName}(lv_color_t value)",
            McuUpdatePropertyKind.BackgroundColor => $"void {functionName}(lv_color_t value)",
            McuUpdatePropertyKind.Font => $"void {functionName}(const lv_font_t * value)",
            _ => $"void {functionName}(int32_t value)"
        };
    }

    private static string GetWrapperInvocation(string unitName, string objectName, McuUpdatePropertyKind property)
    {
        var targetName = McuUpdateModel.CreateTargetEnumName(unitName, objectName, property);
        return property switch
        {
            McuUpdatePropertyKind.Text => $"{unitName}_update_text({targetName}, value)",
            McuUpdatePropertyKind.Value => $"{unitName}_update_value({targetName}, value)",
            McuUpdatePropertyKind.Checked => $"{unitName}_update_checked({targetName}, value)",
            McuUpdatePropertyKind.Visible => $"{unitName}_update_visible({targetName}, value)",
            McuUpdatePropertyKind.Enabled => $"{unitName}_update_enabled({targetName}, value)",
            McuUpdatePropertyKind.TextColor => $"{unitName}_update_text_color({targetName}, value)",
            McuUpdatePropertyKind.BackgroundColor => $"{unitName}_update_background_color({targetName}, value)",
            McuUpdatePropertyKind.Font => $"{unitName}_update_font({targetName}, value)",
            _ => $"{unitName}_update_value({targetName}, value)"
        };
    }

    private static string GetExpectedValueTypeEnum(string unitName, McuUpdatePropertyKind property) =>
        property switch
        {
            McuUpdatePropertyKind.Text => $"{unitName.ToUpperInvariant()}_UPDATE_VALUE_TEXT",
            McuUpdatePropertyKind.Value => $"{unitName.ToUpperInvariant()}_UPDATE_VALUE_INT32",
            McuUpdatePropertyKind.Checked => $"{unitName.ToUpperInvariant()}_UPDATE_VALUE_BOOL",
            McuUpdatePropertyKind.Visible => $"{unitName.ToUpperInvariant()}_UPDATE_VALUE_BOOL",
            McuUpdatePropertyKind.Enabled => $"{unitName.ToUpperInvariant()}_UPDATE_VALUE_BOOL",
            McuUpdatePropertyKind.TextColor => $"{unitName.ToUpperInvariant()}_UPDATE_VALUE_COLOR",
            McuUpdatePropertyKind.BackgroundColor => $"{unitName.ToUpperInvariant()}_UPDATE_VALUE_COLOR",
            McuUpdatePropertyKind.Font => $"{unitName.ToUpperInvariant()}_UPDATE_VALUE_FONT",
            _ => $"{unitName.ToUpperInvariant()}_UPDATE_VALUE_INT32"
        };

    private static string GetPropertyApplyCall(string unitName, McuUpdatePropertyKind property, string targetExpression, string payloadExpression) =>
        property switch
        {
            McuUpdatePropertyKind.Text => $"{unitName}_update_text({targetExpression}, {payloadExpression}.value.text)",
            McuUpdatePropertyKind.Value => $"{unitName}_update_value({targetExpression}, {payloadExpression}.value.int32_value)",
            McuUpdatePropertyKind.Checked => $"{unitName}_update_checked({targetExpression}, {payloadExpression}.value.bool_value)",
            McuUpdatePropertyKind.Visible => $"{unitName}_update_visible({targetExpression}, {payloadExpression}.value.bool_value)",
            McuUpdatePropertyKind.Enabled => $"{unitName}_update_enabled({targetExpression}, {payloadExpression}.value.bool_value)",
            McuUpdatePropertyKind.TextColor => $"{unitName}_update_text_color({targetExpression}, {payloadExpression}.value.color_value)",
            McuUpdatePropertyKind.BackgroundColor => $"{unitName}_update_background_color({targetExpression}, {payloadExpression}.value.color_value)",
            McuUpdatePropertyKind.Font => $"{unitName}_update_font({targetExpression}, {payloadExpression}.value.font_value)",
            _ => $"{unitName}_update_value({targetExpression}, {payloadExpression}.value.int32_value)"
        };

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
}
