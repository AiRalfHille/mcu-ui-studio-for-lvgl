using System.Text;
using System.Text.RegularExpressions;
using Ai.McuUiStudio.Core.MetaModel;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

public sealed class McuEventCodeGenerator
{
    private readonly LvglMetaModelRegistry _metaModelRegistry;
    private const string MessageModeComment = "Build your message here and send it to the controller queue.";

    public McuEventCodeGenerator()
        : this(LvglMetaModelRegistry.CreateDefault())
    {
    }

    public McuEventCodeGenerator(LvglMetaModelRegistry metaModelRegistry)
    {
        _metaModelRegistry = metaModelRegistry;
    }

    public McuEventCodeGenerationResult Generate(UiDocument document, string unitName = "ui_start")
    {
        var safeUnitName = SanitizeIdentifier(unitName, "ui_start");
        var headerFileName = $"{safeUnitName}_event.h";
        var sourceFileName = $"{safeUnitName}_event.c";
        var bindings = new List<McuEventBinding>();

        CollectBindings(document.Root, bindings, safeUnitName);

        var header = BuildHeader(safeUnitName, bindings);
        var source = BuildSource(safeUnitName, headerFileName, bindings);
        var bindCalls = BuildBindingCalls(safeUnitName, bindings);

        return new McuEventCodeGenerationResult(
            headerFileName,
            sourceFileName,
            header,
            source,
            bindCalls,
            bindings.Count > 0);
    }

    private void CollectBindings(UiNode node, List<McuEventBinding> bindings, string unitName)
    {
        var definition = ResolveElementDefinition(node.ElementName);
        if (definition is not null)
        {
            var sourceName = CreateIdentifier(node, definition, definition.Name);
            var objectType = ResolveObjectType(definition);

            foreach (var eventBinding in node.Events.OrderBy(evt => evt.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!eventBinding.Attributes.TryGetValue("callback", out var callbackValue) || string.IsNullOrWhiteSpace(callbackValue))
                {
                    continue;
                }

                bindings.Add(new McuEventBinding(
                    SourceName: sourceName,
                    TriggerName: eventBinding.Name,
                    CallbackValue: callbackValue,
                    EventGroup: eventBinding.Attributes.TryGetValue("eventGroup", out var eventGroup) ? eventGroup ?? string.Empty : string.Empty,
                    EventType: eventBinding.Attributes.TryGetValue("eventType", out var eventType) ? eventType ?? string.Empty : string.Empty,
                    Action: eventBinding.Attributes.TryGetValue("action", out var actionValue) ? actionValue?.Trim() ?? string.Empty : string.Empty,
                    Parameter: ResolveFixedParameter(unitName, objectType, eventBinding.Attributes.TryGetValue("parameter", out var parameterValue) ? parameterValue : null),
                    Value: ResolveRuntimeValue(unitName, objectType),
                    UseMessages: eventBinding.Attributes.TryGetValue("useMessages", out var useMessagesValue) &&
                                 TryParseBoolean(useMessagesValue ?? string.Empty, out var boolValue) && boolValue));
            }
        }

        foreach (var child in node.Children)
        {
            CollectBindings(child, bindings, unitName);
        }
    }

    private string BuildHeader(string unitName, IReadOnlyList<McuEventBinding> bindings)
    {
        var hasSinkBindings = bindings.Any(binding => !binding.UseMessages);
        var header = new StringBuilder();
        header.AppendLine("#pragma once");
        header.AppendLine();
        header.AppendLine("#include <stdbool.h>");
        header.AppendLine("#include <stdint.h>");
        header.AppendLine("#include \"lvgl.h\"");
        header.AppendLine();
        header.AppendLine("typedef enum");
        header.AppendLine("{");
        header.AppendLine($"    {unitName.ToUpperInvariant()}_PARAM_TYPE_NONE = 0,");
        header.AppendLine($"    {unitName.ToUpperInvariant()}_PARAM_TYPE_INT32,");
        header.AppendLine($"    {unitName.ToUpperInvariant()}_PARAM_TYPE_UINT8,");
        header.AppendLine($"    {unitName.ToUpperInvariant()}_PARAM_TYPE_BOOL,");
        header.AppendLine($"    {unitName.ToUpperInvariant()}_PARAM_TYPE_TEXT");
        header.AppendLine($"}} {CreateParameterTypeName(unitName)};");
        header.AppendLine();
        header.AppendLine("typedef enum");
        header.AppendLine("{");
        header.AppendLine($"    {unitName.ToUpperInvariant()}_VALUE_SOURCE_NONE = 0,");
        header.AppendLine($"    {unitName.ToUpperInvariant()}_VALUE_SOURCE_SLIDER_VALUE,");
        header.AppendLine($"    {unitName.ToUpperInvariant()}_VALUE_SOURCE_BAR_VALUE,");
        header.AppendLine($"    {unitName.ToUpperInvariant()}_VALUE_SOURCE_ARC_VALUE,");
        header.AppendLine($"    {unitName.ToUpperInvariant()}_VALUE_SOURCE_SPINBOX_VALUE");
        header.AppendLine($"}} {CreateValueSourceTypeName(unitName)};");
        header.AppendLine();
        header.AppendLine("typedef union");
        header.AppendLine("{");
        header.AppendLine("    int32_t     int32_value;");
        header.AppendLine("    uint8_t     uint8_value;");
        header.AppendLine("    bool        bool_value;");
        header.AppendLine("    const char *text_value;");
        header.AppendLine($"}} {CreateValueUnionTypeName(unitName)};");
        header.AppendLine();
        header.AppendLine("typedef struct");
        header.AppendLine("{");
        header.AppendLine("    const char *source_name;");
        header.AppendLine("    const char *event_group;");
        header.AppendLine("    const char *event_type;");
        header.AppendLine("    const char *action;");
        header.AppendLine($"    {CreateParameterTypeName(unitName)} parameter_type;");
        header.AppendLine($"    {CreateValueUnionTypeName(unitName)} parameter;");
        header.AppendLine($"    {CreateParameterTypeName(unitName)} value_type;");
        header.AppendLine($"    {CreateValueSourceTypeName(unitName)} value_source;");
        header.AppendLine($"    {CreateValueUnionTypeName(unitName)} value;");
        header.AppendLine($"}} {CreateBindingTypeName(unitName)};");
        header.AppendLine();

        if (hasSinkBindings)
        {
            header.AppendLine($"typedef struct");
            header.AppendLine("{");
            header.AppendLine("    const char *source_name;");
            header.AppendLine("    const char *event_group;");
            header.AppendLine("    const char *event_type;");
            header.AppendLine("    const char *action;");
            header.AppendLine($"    {CreateParameterTypeName(unitName)} parameter_type;");
            header.AppendLine($"    {CreateValueUnionTypeName(unitName)} parameter;");
            header.AppendLine($"    {CreateParameterTypeName(unitName)} value_type;");
            header.AppendLine($"    {CreateValueUnionTypeName(unitName)} value;");
            header.AppendLine($"}} {CreateMessageTypeName(unitName)};");
            header.AppendLine();
            header.AppendLine($"typedef bool (*{CreateSinkTypeName(unitName)})({CreateMessageTypeName(unitName)} const * message);");
            header.AppendLine();
            header.AppendLine($"void {unitName}_set_event_sink({CreateSinkTypeName(unitName)} sink);");
            header.AppendLine();
        }

        foreach (var functionName in GetHandlerFunctionNames(unitName, bindings))
        {
            header.AppendLine($"void {functionName}(lv_event_t * e);");
        }

        if (bindings.Count == 0)
        {
            header.AppendLine();
            header.AppendLine("/* No event callbacks configured for this unit. */");
        }

        return header.ToString();
    }

    private string BuildSource(string unitName, string headerFileName, IReadOnlyList<McuEventBinding> bindings)
    {
        var hasSinkBindings = bindings.Any(binding => !binding.UseMessages);
        var source = new StringBuilder();
        source.AppendLine($"#include \"{headerFileName}\"");
        source.AppendLine("#include <string.h>");
        source.AppendLine();

        var groupedBindings = bindings
            .Where(binding => !string.IsNullOrWhiteSpace(binding.EventGroup))
            .GroupBy(binding => binding.EventGroup, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (hasSinkBindings)
        {
            source.AppendLine($"static {CreateSinkTypeName(unitName)} s_event_sink = NULL;");
            source.AppendLine();
            source.AppendLine($"void {unitName}_set_event_sink({CreateSinkTypeName(unitName)} sink)");
            source.AppendLine("{");
            source.AppendLine("    s_event_sink = sink;");
            source.AppendLine("}");
            source.AppendLine();
        }

        if (bindings.Count > 0)
        {
            source.AppendLine($"static int32_t read_runtime_value_int32(lv_event_t * e, {CreateValueSourceTypeName(unitName)} value_source)");
            source.AppendLine("{");
            source.AppendLine("    lv_obj_t *obj = lv_event_get_target_obj(e);");
            source.AppendLine("    if (obj == NULL) return 0;");
            source.AppendLine();
            source.AppendLine("    switch (value_source)");
            source.AppendLine("    {");
            source.AppendLine($"        case {unitName.ToUpperInvariant()}_VALUE_SOURCE_SLIDER_VALUE:");
            source.AppendLine("            return lv_slider_get_value(obj);");
            source.AppendLine($"        case {unitName.ToUpperInvariant()}_VALUE_SOURCE_BAR_VALUE:");
            source.AppendLine("            return lv_bar_get_value(obj);");
            source.AppendLine($"        case {unitName.ToUpperInvariant()}_VALUE_SOURCE_ARC_VALUE:");
            source.AppendLine("            return lv_arc_get_value(obj);");
            source.AppendLine($"        case {unitName.ToUpperInvariant()}_VALUE_SOURCE_SPINBOX_VALUE:");
            source.AppendLine("            return lv_spinbox_get_value(obj);");
            source.AppendLine("        default:");
            source.AppendLine("            return 0;");
            source.AppendLine("    }");
            source.AppendLine("}");
            source.AppendLine();
        }

        if (hasSinkBindings)
        {
            source.AppendLine($"static void dispatch_ui_event(lv_event_t * e, const {CreateBindingTypeName(unitName)} * binding)");
            source.AppendLine("{");
            source.AppendLine("    if(binding == NULL)");
            source.AppendLine("    {");
            source.AppendLine("        return;");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    if(s_event_sink == NULL)");
            source.AppendLine("    {");
                source.AppendLine("        return;");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine($"    {CreateMessageTypeName(unitName)} msg =");
            source.AppendLine("    {");
            source.AppendLine("        .source_name = binding->source_name,");
            source.AppendLine("        .event_group = binding->event_group,");
            source.AppendLine("        .event_type = binding->event_type,");
            source.AppendLine("        .action = binding->action,");
            source.AppendLine("        .parameter_type = binding->parameter_type,");
            source.AppendLine("        .parameter = binding->parameter,");
            source.AppendLine("        .value_type = binding->value_type,");
            source.AppendLine("        .value = binding->value");
            source.AppendLine("    };");
            source.AppendLine();
            source.AppendLine($"    if (binding->value_source != {unitName.ToUpperInvariant()}_VALUE_SOURCE_NONE)");
            source.AppendLine("    {");
            source.AppendLine("        msg.value.int32_value = read_runtime_value_int32(e, binding->value_source);");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    (void)s_event_sink(&msg);");
            source.AppendLine("}");
            source.AppendLine();
        }

        var emittedIndividualFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var binding in bindings.Where(binding => string.IsNullOrWhiteSpace(binding.EventGroup)))
        {
            var functionName = CreateIndividualCallbackName(unitName, binding.CallbackValue, binding.SourceName, binding.TriggerName);
            if (!emittedIndividualFunctions.Add(functionName))
            {
                continue;
            }

            var relatedBindings = bindings
                .Where(candidate => string.IsNullOrWhiteSpace(candidate.EventGroup) &&
                                    string.Equals(CreateIndividualCallbackName(unitName, candidate.CallbackValue, candidate.SourceName, candidate.TriggerName), functionName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            source.AppendLine($"void {functionName}(lv_event_t * e)");
            source.AppendLine("{");
            source.AppendLine("    if(e == NULL)");
            source.AppendLine("    {");
            source.AppendLine("        return;");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine($"    {CreateBindingTypeName(unitName)} * binding = ({CreateBindingTypeName(unitName)} *)lv_event_get_user_data(e);");
            source.AppendLine("    if(binding == NULL)");
            source.AppendLine("    {");
            source.AppendLine("        return;");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    switch(lv_event_get_code(e))");
            source.AppendLine("    {");

            foreach (var triggerGroup in relatedBindings
                         .GroupBy(candidate => candidate.TriggerName, StringComparer.OrdinalIgnoreCase)
                         .OrderBy(candidate => candidate.Key, StringComparer.OrdinalIgnoreCase))
            {
                source.AppendLine($"        case {ToLvEventCodeExpression(triggerGroup.Key)}:");
                source.AppendLine("        {");

                foreach (var candidate in triggerGroup.OrderBy(item => item.SourceName, StringComparer.OrdinalIgnoreCase))
                {
                    source.AppendLine($"            if(strcmp(binding->source_name, {ToCString(candidate.SourceName)}) == 0)");
                    source.AppendLine("            {");
                    if (candidate.UseMessages)
                    {
                        AppendMessageExampleBlock(source, candidate, 4, unitName);
                    }
                    else
                    {
                        source.AppendLine("                dispatch_ui_event(e, binding);");
                    }
                    source.AppendLine("                return;");
                    source.AppendLine("            }");
                }

                source.AppendLine("            break;");
                source.AppendLine("        }");
            }

            source.AppendLine("        default:");
            source.AppendLine("            break;");
            source.AppendLine("    }");
            source.AppendLine("}");
            source.AppendLine();
        }

        foreach (var group in groupedBindings)
        {
            var dispatcherName = CreateGroupDispatcherName(unitName, group.Key);

            source.AppendLine($"void {dispatcherName}(lv_event_t * e)");
            source.AppendLine("{");
            source.AppendLine("    if(e == NULL)");
            source.AppendLine("    {");
            source.AppendLine("        return;");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine($"    {CreateBindingTypeName(unitName)} * binding = ({CreateBindingTypeName(unitName)} *)lv_event_get_user_data(e);");
            source.AppendLine("    if(binding == NULL)");
            source.AppendLine("    {");
            source.AppendLine("        return;");
            source.AppendLine("    }");
            source.AppendLine();
            source.AppendLine("    switch(lv_event_get_code(e))");
            source.AppendLine("    {");

            foreach (var triggerGroup in group.GroupBy(binding => binding.TriggerName, StringComparer.OrdinalIgnoreCase).OrderBy(bindingGroup => bindingGroup.Key, StringComparer.OrdinalIgnoreCase))
            {
                source.AppendLine($"        case {ToLvEventCodeExpression(triggerGroup.Key)}:");
                source.AppendLine("        {");

                foreach (var eventTypeGroup in triggerGroup.GroupBy(binding => binding.EventType, StringComparer.OrdinalIgnoreCase).OrderBy(bindingGroup => bindingGroup.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(eventTypeGroup.Key))
                    {
                        source.AppendLine($"            if(strcmp(binding->event_type, {ToCString(eventTypeGroup.Key)}) == 0)");
                        source.AppendLine("            {");

                        foreach (var binding in eventTypeGroup.OrderBy(candidate => candidate.SourceName, StringComparer.OrdinalIgnoreCase))
                        {
                            source.AppendLine($"                if(strcmp(binding->source_name, {ToCString(binding.SourceName)}) == 0)");
                            source.AppendLine("                {");
                            if (binding.UseMessages)
                            {
                                AppendMessageExampleBlock(source, binding, 5, unitName);
                            }
                            else
                            {
                                source.AppendLine("                    dispatch_ui_event(e, binding);");
                            }
                            source.AppendLine("                    return;");
                            source.AppendLine("                }");
                        }

                        source.AppendLine("            }");
                    }
                    else
                    {
                        foreach (var binding in eventTypeGroup.OrderBy(candidate => candidate.SourceName, StringComparer.OrdinalIgnoreCase))
                        {
                            source.AppendLine($"            if(strcmp(binding->source_name, {ToCString(binding.SourceName)}) == 0)");
                            source.AppendLine("            {");
                            if (binding.UseMessages)
                            {
                                AppendMessageExampleBlock(source, binding, 4, unitName);
                            }
                            else
                            {
                                source.AppendLine("                dispatch_ui_event(e, binding);");
                            }
                            source.AppendLine("                return;");
                            source.AppendLine("            }");
                        }
                    }
                }

                source.AppendLine("            break;");
                source.AppendLine("        }");
            }

            source.AppendLine("        default:");
            source.AppendLine("            break;");
            source.AppendLine("    }");
            source.AppendLine("}");
            source.AppendLine();
        }

        if (bindings.Count == 0)
        {
            source.AppendLine("/* No event callbacks configured for this unit. */");
        }

        return source.ToString();
    }

    private string BuildBindingCalls(string unitName, IReadOnlyList<McuEventBinding> bindings)
    {
        if (bindings.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var binding in bindings
                     .OrderBy(candidate => candidate.SourceName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(candidate => candidate.TriggerName, StringComparer.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(binding.EventGroup))
            {
                builder.AppendLine($"    static {CreateBindingTypeName(unitName)} {CreateLocalBindingName(binding)} =");
                builder.AppendLine("    {");
                builder.AppendLine($"        .source_name = {ToCString(binding.SourceName)},");
                builder.AppendLine($"        .event_group = {ToCString(binding.EventGroup)},");
                builder.AppendLine($"        .event_type = {ToCString(binding.EventType)},");
                builder.AppendLine($"        .action = {ToCString(binding.Action)},");
                builder.AppendLine($"        .parameter_type = {binding.Parameter.TypeEnumConstantName},");
                builder.AppendLine($"        .parameter = {{ {binding.Parameter.Initializer} }},");
                builder.AppendLine($"        .value_type = {binding.Value.TypeEnumConstantName},");
                builder.AppendLine($"        .value_source = {binding.Value.SourceEnumConstantName},");
                builder.AppendLine($"        .value = {{ {binding.Value.Initializer} }}");
                builder.AppendLine("    };");
                builder.AppendLine($"    lv_obj_add_event_cb({binding.SourceName}, {CreateGroupDispatcherName(unitName, binding.EventGroup)}, {ToLvEventCodeExpression(binding.TriggerName)}, &{CreateLocalBindingName(binding)});");
            }
            else
            {
                builder.AppendLine($"    static {CreateBindingTypeName(unitName)} {CreateLocalBindingName(binding)} =");
                builder.AppendLine("    {");
                builder.AppendLine($"        .source_name = {ToCString(binding.SourceName)},");
                builder.AppendLine("        .event_group = \"\",");
                builder.AppendLine("        .event_type = \"\",");
                builder.AppendLine($"        .action = {ToCString(binding.Action)},");
                builder.AppendLine($"        .parameter_type = {binding.Parameter.TypeEnumConstantName},");
                builder.AppendLine($"        .parameter = {{ {binding.Parameter.Initializer} }},");
                builder.AppendLine($"        .value_type = {binding.Value.TypeEnumConstantName},");
                builder.AppendLine($"        .value_source = {binding.Value.SourceEnumConstantName},");
                builder.AppendLine($"        .value = {{ {binding.Value.Initializer} }}");
                builder.AppendLine("    };");
                builder.AppendLine($"    lv_obj_add_event_cb({binding.SourceName}, {CreateIndividualCallbackName(unitName, binding.CallbackValue, binding.SourceName, binding.TriggerName)}, {ToLvEventCodeExpression(binding.TriggerName)}, &{CreateLocalBindingName(binding)});");
            }
        }

        return builder.ToString().TrimEnd();
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

    private static string CreateBindingTypeName(string unitName) =>
        $"{SanitizeIdentifier(unitName, "ui_start")}_event_binding_t";

    private static string CreateMessageTypeName(string unitName) =>
        $"{SanitizeIdentifier(unitName, "ui_start")}_event_message_t";

    private static string CreateSinkTypeName(string unitName) =>
        $"{SanitizeIdentifier(unitName, "ui_start")}_event_sink_t";

    private static string CreateParameterTypeName(string unitName) =>
        $"{SanitizeIdentifier(unitName, "ui_start")}_parameter_type_t";

    private static string CreateValueSourceTypeName(string unitName) =>
        $"{SanitizeIdentifier(unitName, "ui_start")}_value_source_t";

    private static string CreateValueUnionTypeName(string unitName) =>
        $"{SanitizeIdentifier(unitName, "ui_start")}_message_value_t";

    private static void AppendMessageExampleBlock(StringBuilder source, McuEventBinding binding, int indentLevel, string unitName)
    {
        var indent = Indent(indentLevel);
        source.AppendLine($"{indent}/*");
        source.AppendLine($"{indent} * TODO: {MessageModeComment}");
        if (!string.IsNullOrWhiteSpace(binding.EventType))
        {
            source.AppendLine($"{indent} * eventGroup='{binding.EventGroup}', eventType='{binding.EventType}', source='{binding.SourceName}', action='{binding.Action}'");
        }
        else if (!string.IsNullOrWhiteSpace(binding.EventGroup))
        {
            source.AppendLine($"{indent} * eventGroup='{binding.EventGroup}', source='{binding.SourceName}', action='{binding.Action}'");
        }
        else
        {
            source.AppendLine($"{indent} * source='{binding.SourceName}', action='{binding.Action}'");
        }
        source.AppendLine($"{indent} *");
        source.AppendLine($"{indent} * Example:");
        source.AppendLine($"{indent} * ui_message_t msg;");
        source.AppendLine($"{indent} * msg.source_name = binding->source_name;");
        source.AppendLine($"{indent} * msg.event_group = binding->event_group;");
        source.AppendLine($"{indent} * msg.event_type = binding->event_type;");
        source.AppendLine($"{indent} * msg.action = binding->action;");
        source.AppendLine($"{indent} * msg.parameter_type = binding->parameter_type;");
        source.AppendLine($"{indent} * msg.parameter = binding->parameter;");
        source.AppendLine($"{indent} * msg.value_type = binding->value_type;");
        source.AppendLine($"{indent} * msg.value = binding->value;");
        source.AppendLine($"{indent} * if (binding->value_source != {unitName.ToUpperInvariant()}_VALUE_SOURCE_NONE)");
        source.AppendLine($"{indent} * {{");
        source.AppendLine($"{indent} *     msg.value.int32_value = read_runtime_value_int32(e, binding->value_source);");
        source.AppendLine($"{indent} * }}");
        source.AppendLine($"{indent} *");
        source.AppendLine($"{indent} * xQueueSend(g_ui_to_controller_queue, &msg, 0);");
        source.AppendLine($"{indent} */");
    }

    private static string ResolveObjectType(LvglElementDefinition definition)
    {
        var name = definition.Name.ToLowerInvariant();
        return name switch
        {
            "label" => "label",
            "led" => "led",
            "bar" => "bar",
            "slider" => "slider",
            "arc" => "arc",
            "spinbox" => "spinbox",
            "switch" => "switch",
            "checkbox" => "checkbox",
            _ => "none"
        };
    }

    private static McuEventParameter ResolveFixedParameter(string unitName, string objectType, string? parameterValue)
    {
        if (string.IsNullOrWhiteSpace(parameterValue))
        {
            return new McuEventParameter(
                $"{unitName.ToUpperInvariant()}_PARAM_TYPE_NONE",
                ".int32_value = 0");
        }

        var trimmed = parameterValue.Trim();
        if (bool.TryParse(trimmed, out var boolValue))
        {
            return new McuEventParameter(
                $"{unitName.ToUpperInvariant()}_PARAM_TYPE_BOOL",
                $".bool_value = {(boolValue ? "true" : "false")}");
        }

        if (string.Equals(objectType, "led", StringComparison.OrdinalIgnoreCase) &&
            byte.TryParse(trimmed, out var byteValue))
        {
            return new McuEventParameter(
                $"{unitName.ToUpperInvariant()}_PARAM_TYPE_UINT8",
                $".uint8_value = {byteValue}");
        }

        if (int.TryParse(trimmed, out var intValue))
        {
            return new McuEventParameter(
                $"{unitName.ToUpperInvariant()}_PARAM_TYPE_INT32",
                $".int32_value = {intValue}");
        }

        return new McuEventParameter(
            $"{unitName.ToUpperInvariant()}_PARAM_TYPE_TEXT",
            $".text_value = {ToCString(trimmed)}");
    }

    private static McuEventValue ResolveRuntimeValue(string unitName, string objectType) => objectType switch
    {
        "slider" => new McuEventValue($"{unitName.ToUpperInvariant()}_PARAM_TYPE_INT32", $"{unitName.ToUpperInvariant()}_VALUE_SOURCE_SLIDER_VALUE", ".int32_value = 0"),
        "bar" => new McuEventValue($"{unitName.ToUpperInvariant()}_PARAM_TYPE_INT32", $"{unitName.ToUpperInvariant()}_VALUE_SOURCE_BAR_VALUE", ".int32_value = 0"),
        "arc" => new McuEventValue($"{unitName.ToUpperInvariant()}_PARAM_TYPE_INT32", $"{unitName.ToUpperInvariant()}_VALUE_SOURCE_ARC_VALUE", ".int32_value = 0"),
        "spinbox" => new McuEventValue($"{unitName.ToUpperInvariant()}_PARAM_TYPE_INT32", $"{unitName.ToUpperInvariant()}_VALUE_SOURCE_SPINBOX_VALUE", ".int32_value = 0"),
        _ => new McuEventValue($"{unitName.ToUpperInvariant()}_PARAM_TYPE_NONE", $"{unitName.ToUpperInvariant()}_VALUE_SOURCE_NONE", ".int32_value = 0")
    };

    private static IEnumerable<string> GetHandlerFunctionNames(string unitName, IReadOnlyList<McuEventBinding> bindings)
    {
        return bindings
            .Select(binding => !string.IsNullOrWhiteSpace(binding.EventGroup)
                ? CreateGroupDispatcherName(unitName, binding.EventGroup)
                : CreateIndividualCallbackName(unitName, binding.CallbackValue, binding.SourceName, binding.TriggerName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase);
    }

    private static string CreateGroupDispatcherName(string unitName, string eventGroup) =>
        $"{SanitizeIdentifier(unitName, "ui_start")}_{SanitizeIdentifier(eventGroup, "group")}_dispatcher";

    private static string CreateIndividualCallbackName(string unitName, string callbackValue, string sourceName, string triggerName)
    {
        var callbackIdentifier = SanitizeIdentifier(callbackValue, $"{sourceName}_{triggerName}");
        return $"{SanitizeIdentifier(unitName, "ui_start")}_{callbackIdentifier}";
    }

    private static string CreateLocalBindingName(McuEventBinding binding) =>
        $"{SanitizeIdentifier(binding.SourceName, "obj")}_{SanitizeIdentifier(binding.TriggerName, "event")}_binding";

    private string CreateIdentifier(UiNode node, LvglElementDefinition definition, string fallbackPrefix)
    {
        var candidate = node.Attributes.GetValueOrDefault("id")
                        ?? node.Attributes.GetValueOrDefault("name")
                        ?? fallbackPrefix;

        return SanitizeIdentifier(candidate, fallbackPrefix);
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

    private static string ToLvEventCodeExpression(string eventName) =>
        $"LV_EVENT_{eventName.Trim().Replace('-', '_').Replace(' ', '_').ToUpperInvariant()}";

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

    private static string Indent(int level) => new(' ', Math.Max(0, level) * 4);

    private sealed record McuEventBinding(
        string SourceName,
        string TriggerName,
        string CallbackValue,
        string EventGroup,
        string EventType,
        string Action,
        McuEventParameter Parameter,
        McuEventValue Value,
        bool UseMessages);

    private sealed record McuEventParameter(
        string TypeEnumConstantName,
        string Initializer);

    private sealed record McuEventValue(
        string TypeEnumConstantName,
        string SourceEnumConstantName,
        string Initializer);
}
