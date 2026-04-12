using System.Text;
using System.Text.RegularExpressions;
using Ai.McuUiStudio.Core.MetaModel;
using Ai.McuUiStudio.Core.Model;

namespace Ai.McuUiStudio.Core.Services;

public sealed class RtosMessagesCodeGenerator
{
    private readonly LvglMetaModelRegistry _metaModelRegistry;

    public RtosMessagesCodeGenerator()
        : this(LvglMetaModelRegistry.CreateDefault())
    {
    }

    public RtosMessagesCodeGenerator(LvglMetaModelRegistry metaModelRegistry)
    {
        _metaModelRegistry = metaModelRegistry;
    }

    public RtosMessagesCodeGenerationResult Generate(UiDocument document, string unitName = "ui_start")
    {
        var safeUnitName = SanitizeIdentifier(unitName, "ui_start");
        var contract = BuildModel(document, safeUnitName);

        return new RtosMessagesCodeGenerationResult(
            $"{safeUnitName}_contract.h",
            BuildContractHeader(contract),
            $"{safeUnitName}_event.c",
            BuildEventSource(contract),
            $"{safeUnitName}_update.c",
            BuildUpdateSource(contract),
            BuildBindingCalls(contract),
            contract.Objects.Select(x => x.Id).ToArray());
    }

    private ScreenContractModel BuildModel(UiDocument document, string unitName)
    {
        var objects = new List<ContractObject>();
        var events = new List<ContractEvent>();
        var actions = new Dictionary<string, ContractAction>(StringComparer.OrdinalIgnoreCase);
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var screenName = document.Root.Attributes.TryGetValue("name", out var rootName) && !string.IsNullOrWhiteSpace(rootName)
            ? rootName
            : unitName;

        Traverse(document.Root, objects, events, actions, usedIds, unitName);

        return new ScreenContractModel(unitName, screenName, objects, actions.Values.OrderBy(x => x.EnumConstantName, StringComparer.OrdinalIgnoreCase).ToArray(), events);
    }

    private void Traverse(
        UiNode node,
        List<ContractObject> objects,
        List<ContractEvent> events,
        IDictionary<string, ContractAction> actions,
        HashSet<string> usedIds,
        string unitName)
    {
        var definition = ResolveElementDefinition(node.ElementName);
        if (definition is not null)
        {
            var id = GetAttributeValue(node.Attributes, "id");
            ContractObject? contractObject = null;

            if (!string.IsNullOrWhiteSpace(id))
            {
                if (!usedIds.Add(id))
                {
                    throw new InvalidOperationException($"Duplicate RTOS contract id '{id}' detected.");
                }

                var useUpdate = GetAttributeValue(node.Attributes, "useUpdate");
                contractObject = new ContractObject(
                    Id: id,
                    HandleName: SanitizeIdentifier(id, "obj"),
                    EnumConstantName: SanitizeIdentifier(id, "obj").ToUpperInvariant(),
                    ObjectType: ResolveObjectType(definition),
                    UseUpdate: string.Equals(useUpdate, "true", StringComparison.OrdinalIgnoreCase));
                objects.Add(contractObject);
            }

            foreach (var evt in node.Events.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (contractObject is null)
                {
                    throw new InvalidOperationException($"RTOS-Messages requires an id on event source '{definition.Name}'.");
                }

                if (!evt.Attributes.TryGetValue("callback", out var callback) || string.IsNullOrWhiteSpace(callback))
                {
                    continue;
                }

                if (!evt.Attributes.TryGetValue("action", out var action) || string.IsNullOrWhiteSpace(action))
                {
                    throw new InvalidOperationException($"RTOS-Messages requires an action on event '{evt.Name}' for object id '{contractObject.Id}'.");
                }

                var actionValue = action.Trim();
                if (!actions.TryGetValue(actionValue, out var contractAction))
                {
                    contractAction = new ContractAction(
                        Value: actionValue,
                        EnumConstantName: CreateActionEnumConstantName(unitName, actionValue));
                    actions[actionValue] = contractAction;
                }

                events.Add(new ContractEvent(
                    ObjectId: contractObject.Id,
                    ObjectHandleName: contractObject.HandleName,
                    ObjectEnumConstantName: contractObject.EnumConstantName,
                    ObjectType: contractObject.ObjectType,
                    TriggerName: evt.Name,
                    Action: contractAction.Value,
                    ActionEnumConstantName: contractAction.EnumConstantName,
                    Parameter: ResolveFixedParameter(unitName, contractObject.ObjectType, evt.Attributes.TryGetValue("parameter", out var parameterValue) ? parameterValue : null),
                    Value: ResolveRuntimeValue(unitName, contractObject.ObjectType),
                    BindingVariableName: $"{contractObject.HandleName}_{SanitizeIdentifier(evt.Name, "event")}_binding"));
            }
        }

        foreach (var child in node.Children)
        {
            Traverse(child, objects, events, actions, usedIds, unitName);
        }
    }

    private static string BuildContractHeader(ScreenContractModel model)
    {
        var b = new StringBuilder();
        b.AppendLine("#pragma once");
        b.AppendLine();
        b.AppendLine("/* --------------------------------------------------------------------------");
        b.AppendLine("   GENERIERT — nicht von Hand ändern");
        b.AppendLine($"   Quelle: Screen \"{model.ScreenName}\"");
        b.AppendLine();
        b.AppendLine("   Contract zwischen Display und Controller.");
        b.AppendLine("   Diese Datei beschreibt alle Objekte des Screens die interagieren —");
        b.AppendLine("   ausgehende Events (Display → Controller) und");
        b.AppendLine("   eingehende Updates (Controller → Display).");
        b.AppendLine("   -------------------------------------------------------------------------- */");
        b.AppendLine();
        b.AppendLine("#include <stdbool.h>");
        b.AppendLine("#include <stdint.h>");
        b.AppendLine("#include \"lvgl.h\"");
        b.AppendLine();
        b.AppendLine("typedef enum");
        b.AppendLine("{");
        if (model.Objects.Count == 0)
        {
            b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_OBJ_COUNT = 0");
        }
        else
        {
            for (var i = 0; i < model.Objects.Count; i++)
            {
                var suffix = i == model.Objects.Count - 1 ? "," : ",";
                var initializer = i == 0 ? " = 0" : string.Empty;
                b.AppendLine($"    {model.Objects[i].EnumConstantName}{initializer}{suffix}");
            }
            b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_OBJ_COUNT");
        }
        b.AppendLine($"}} {model.UnitName}_object_t;");
        b.AppendLine();
        b.AppendLine("typedef enum");
        b.AppendLine("{");
        if (model.Actions.Count == 0)
        {
            b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_ACTION_COUNT = 0");
        }
        else
        {
            for (var i = 0; i < model.Actions.Count; i++)
            {
                var initializer = i == 0 ? " = 0" : string.Empty;
                b.AppendLine($"    {model.Actions[i].EnumConstantName}{initializer},");
            }
            b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_ACTION_COUNT");
        }
        b.AppendLine($"}} {model.UnitName}_action_t;");
        b.AppendLine();
        b.AppendLine("typedef enum");
        b.AppendLine("{");
        b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_PARAM_TYPE_NONE = 0,");
        b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_PARAM_TYPE_INT32,");
        b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_PARAM_TYPE_UINT8,");
        b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_PARAM_TYPE_BOOL,");
        b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_PARAM_TYPE_TEXT");
        b.AppendLine($"}} {model.UnitName}_parameter_type_t;");
        b.AppendLine();
        b.AppendLine("typedef enum");
        b.AppendLine("{");
        b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_VALUE_SOURCE_NONE = 0,");
        b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_VALUE_SOURCE_SLIDER_VALUE,");
        b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_VALUE_SOURCE_BAR_VALUE,");
        b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_VALUE_SOURCE_ARC_VALUE,");
        b.AppendLine($"    {model.UnitName.ToUpperInvariant()}_VALUE_SOURCE_SPINBOX_VALUE");
        b.AppendLine($"}} {model.UnitName}_value_source_t;");
        b.AppendLine();
        b.AppendLine("typedef union");
        b.AppendLine("{");
        b.AppendLine("    int32_t     int32_value;");
        b.AppendLine("    uint8_t     uint8_value;");
        b.AppendLine("    bool        bool_value;");
        b.AppendLine("    const char *text_value;");
        b.AppendLine($"}} {model.UnitName}_message_value_t;");
        b.AppendLine();
        b.AppendLine("typedef struct");
        b.AppendLine("{");
        b.AppendLine($"    {model.UnitName}_object_t  source;");
        b.AppendLine($"    {model.UnitName}_action_t action;");
        b.AppendLine($"    {model.UnitName}_parameter_type_t parameter_type;");
        b.AppendLine($"    {model.UnitName}_message_value_t parameter;");
        b.AppendLine($"    {model.UnitName}_parameter_type_t value_type;");
        b.AppendLine($"    {model.UnitName}_message_value_t value;");
        b.AppendLine("} app_message_t;");
        b.AppendLine();
        b.AppendLine("typedef struct");
        b.AppendLine("{");
        b.AppendLine($"    {model.UnitName}_object_t  source;");
        b.AppendLine($"    {model.UnitName}_action_t action;");
        b.AppendLine($"    {model.UnitName}_parameter_type_t parameter_type;");
        b.AppendLine($"    {model.UnitName}_message_value_t parameter;");
        b.AppendLine($"    {model.UnitName}_parameter_type_t value_type;");
        b.AppendLine($"    {model.UnitName}_value_source_t value_source;");
        b.AppendLine($"    {model.UnitName}_message_value_t value;");
        b.AppendLine($"}} {model.UnitName}_event_binding_t;");
        b.AppendLine();
        b.AppendLine($"void {model.UnitName}_dispatcher(lv_event_t *e);");
        b.AppendLine($"void {model.UnitName}_update_text({model.UnitName}_object_t target, const char *value);");
        b.AppendLine($"void {model.UnitName}_update_value({model.UnitName}_object_t target, int32_t value);");
        b.AppendLine($"void {model.UnitName}_update_bool({model.UnitName}_object_t target, bool value);");
        b.AppendLine($"void {model.UnitName}_update_brightness({model.UnitName}_object_t target, uint8_t value);");
        b.AppendLine($"void {model.UnitName}_update_visible({model.UnitName}_object_t target, bool value);");
        b.AppendLine($"void {model.UnitName}_update_enabled({model.UnitName}_object_t target, bool value);");
        return b.ToString();
    }

    private static string BuildEventSource(ScreenContractModel model)
    {
        var b = new StringBuilder();
        b.AppendLine("/* --------------------------------------------------------------------------");
        b.AppendLine("   GENERIERT — nicht von Hand ändern");
        b.AppendLine($"   Quelle: Screen \"{model.ScreenName}\", TemplateType = RTOS-Messages");
        b.AppendLine("   -------------------------------------------------------------------------- */");
        b.AppendLine();
        b.AppendLine($"#include \"{model.UnitName}_contract.h\"");
        b.AppendLine("#include \"message.h\"");
        b.AppendLine();
        b.AppendLine("static int32_t read_runtime_value_int32(lv_event_t *e, " +
                     $"{model.UnitName}_value_source_t value_source)");
        b.AppendLine("{");
        b.AppendLine("    lv_obj_t *obj = lv_event_get_target_obj(e);");
        b.AppendLine("    if (obj == NULL) return 0;");
        b.AppendLine();
        b.AppendLine("    switch (value_source)");
        b.AppendLine("    {");
        b.AppendLine($"        case {model.UnitName.ToUpperInvariant()}_VALUE_SOURCE_SLIDER_VALUE:");
        b.AppendLine("            return lv_slider_get_value(obj);");
        b.AppendLine($"        case {model.UnitName.ToUpperInvariant()}_VALUE_SOURCE_BAR_VALUE:");
        b.AppendLine("            return lv_bar_get_value(obj);");
        b.AppendLine($"        case {model.UnitName.ToUpperInvariant()}_VALUE_SOURCE_ARC_VALUE:");
        b.AppendLine("            return lv_arc_get_value(obj);");
        b.AppendLine($"        case {model.UnitName.ToUpperInvariant()}_VALUE_SOURCE_SPINBOX_VALUE:");
        b.AppendLine("            return lv_spinbox_get_value(obj);");
        b.AppendLine("        default:");
        b.AppendLine("            return 0;");
        b.AppendLine("    }");
        b.AppendLine("}");
        b.AppendLine();
        b.AppendLine($"void {model.UnitName}_dispatcher(lv_event_t *e)");
        b.AppendLine("{");
        b.AppendLine("    if (e == NULL) return;");
        b.AppendLine();
        b.AppendLine($"    {model.UnitName}_event_binding_t *binding =");
        b.AppendLine($"        ({model.UnitName}_event_binding_t *)lv_event_get_user_data(e);");
        b.AppendLine();
        b.AppendLine("    if (binding == NULL) return;");
        b.AppendLine();
        b.AppendLine("    control_message_t msg = {");
        b.AppendLine("        .source_type            = CONTROL_MSG_FROM_DISPLAY,");
        b.AppendLine("        .payload.display.source = binding->source,");
        b.AppendLine("        .payload.display.action = binding->action,");
        b.AppendLine("        .payload.display.parameter_type = binding->parameter_type,");
        b.AppendLine("        .payload.display.parameter = binding->parameter,");
        b.AppendLine("        .payload.display.value_type = binding->value_type,");
        b.AppendLine("        .payload.display.value = binding->value");
        b.AppendLine("    };");
        b.AppendLine();
        b.AppendLine("    if (binding->value_source != " +
                     $"{model.UnitName.ToUpperInvariant()}_VALUE_SOURCE_NONE)");
        b.AppendLine("    {");
        b.AppendLine("        msg.payload.display.value.int32_value =");
        b.AppendLine("            read_runtime_value_int32(e, binding->value_source);");
        b.AppendLine("    }");
        b.AppendLine();
        b.AppendLine("    xQueueSend(control_queue, &msg, 0);");
        b.AppendLine("}");
        return b.ToString();
    }

    private static string BuildUpdateSource(ScreenContractModel model)
    {
        var b = new StringBuilder();
        b.AppendLine("/* --------------------------------------------------------------------------");
        b.AppendLine("   GENERIERT — nicht von Hand ändern");
        b.AppendLine($"   Quelle: Screen \"{model.ScreenName}\", TemplateType = RTOS-Messages");
        b.AppendLine("   -------------------------------------------------------------------------- */");
        b.AppendLine();
        b.AppendLine($"#include \"{model.UnitName}_contract.h\"");
        b.AppendLine($"#include \"{model.UnitName}.h\"");
        b.AppendLine();
        b.AppendLine("typedef enum");
        b.AppendLine("{");
        b.AppendLine("    UI_OBJ_TYPE_NONE = 0,");
        b.AppendLine("    UI_OBJ_TYPE_LABEL,");
        b.AppendLine("    UI_OBJ_TYPE_LED,");
        b.AppendLine("    UI_OBJ_TYPE_BAR,");
        b.AppendLine("    UI_OBJ_TYPE_SLIDER,");
        b.AppendLine("    UI_OBJ_TYPE_ARC,");
        b.AppendLine("    UI_OBJ_TYPE_SPINBOX,");
        b.AppendLine("    UI_OBJ_TYPE_SWITCH,");
        b.AppendLine("    UI_OBJ_TYPE_CHECKBOX,");
        b.AppendLine("} ui_obj_type_t;");
        b.AppendLine();
        var updateObjects = model.Objects.Where(x => x.UseUpdate).ToArray();
        b.AppendLine($"static lv_obj_t **{model.UnitName}_objects[{model.UnitName.ToUpperInvariant()}_OBJ_COUNT] = {{");
        foreach (var obj in updateObjects)
        {
            b.AppendLine($"    [{obj.EnumConstantName}] = &{obj.HandleName},");
        }
        b.AppendLine("};");
        b.AppendLine();
        b.AppendLine($"static const ui_obj_type_t {model.UnitName}_object_types[{model.UnitName.ToUpperInvariant()}_OBJ_COUNT] = {{");
        foreach (var obj in updateObjects)
        {
            b.AppendLine($"    [{obj.EnumConstantName}] = {ToTypeEnum(obj.ObjectType)},");
        }
        b.AppendLine("};");
        b.AppendLine();
        b.AppendLine($"static lv_obj_t *resolve({model.UnitName}_object_t target)");
        b.AppendLine("{");
        b.AppendLine($"    if (target >= {model.UnitName.ToUpperInvariant()}_OBJ_COUNT) return NULL;");
        b.AppendLine($"    return {model.UnitName}_objects[target] ? *{model.UnitName}_objects[target] : NULL;");
        b.AppendLine("}");
        b.AppendLine();
        b.AppendLine($"void {model.UnitName}_update_text({model.UnitName}_object_t target, const char *value)");
        b.AppendLine("{");
        b.AppendLine("    if (value == NULL) return;");
        b.AppendLine("    lv_obj_t *obj = resolve(target);");
        b.AppendLine("    if (obj == NULL) return;");
        b.AppendLine($"    if ({model.UnitName}_object_types[target] == UI_OBJ_TYPE_LABEL)");
        b.AppendLine("        lv_label_set_text(obj, value);");
        b.AppendLine("}");
        b.AppendLine();
        b.AppendLine($"void {model.UnitName}_update_value({model.UnitName}_object_t target, int32_t value)");
        b.AppendLine("{");
        b.AppendLine("    lv_obj_t *obj = resolve(target);");
        b.AppendLine("    if (obj == NULL) return;");
        b.AppendLine($"    switch ({model.UnitName}_object_types[target])");
        b.AppendLine("    {");
        b.AppendLine("        case UI_OBJ_TYPE_BAR:    lv_bar_set_value(obj, value, LV_ANIM_OFF); break;");
        b.AppendLine("        case UI_OBJ_TYPE_SLIDER: lv_slider_set_value(obj, value, LV_ANIM_OFF); break;");
        b.AppendLine("        case UI_OBJ_TYPE_ARC:    lv_arc_set_value(obj, value); break;");
        b.AppendLine("        case UI_OBJ_TYPE_SPINBOX: lv_spinbox_set_value(obj, value); break;");
        b.AppendLine("        default: break;");
        b.AppendLine("    }");
        b.AppendLine("}");
        b.AppendLine();
        b.AppendLine($"void {model.UnitName}_update_bool({model.UnitName}_object_t target, bool value)");
        b.AppendLine("{");
        b.AppendLine("    lv_obj_t *obj = resolve(target);");
        b.AppendLine("    if (obj == NULL) return;");
        b.AppendLine($"    switch ({model.UnitName}_object_types[target])");
        b.AppendLine("    {");
        b.AppendLine("        case UI_OBJ_TYPE_LED:");
        b.AppendLine("            if (value) lv_led_on(obj); else lv_led_off(obj);");
        b.AppendLine("            break;");
        b.AppendLine("        case UI_OBJ_TYPE_SWITCH:");
        b.AppendLine("        case UI_OBJ_TYPE_CHECKBOX:");
        b.AppendLine("            if (value) lv_obj_add_state(obj, LV_STATE_CHECKED);");
        b.AppendLine("            else       lv_obj_remove_state(obj, LV_STATE_CHECKED);");
        b.AppendLine("            break;");
        b.AppendLine("        default: break;");
        b.AppendLine("    }");
        b.AppendLine("}");
        b.AppendLine();
        b.AppendLine($"void {model.UnitName}_update_brightness({model.UnitName}_object_t target, uint8_t value)");
        b.AppendLine("{");
        b.AppendLine("    lv_obj_t *obj = resolve(target);");
        b.AppendLine("    if (obj == NULL) return;");
        b.AppendLine($"    if ({model.UnitName}_object_types[target] == UI_OBJ_TYPE_LED)");
        b.AppendLine("        lv_led_set_brightness(obj, value);");
        b.AppendLine("}");
        b.AppendLine();
        b.AppendLine($"void {model.UnitName}_update_visible({model.UnitName}_object_t target, bool value)");
        b.AppendLine("{");
        b.AppendLine("    lv_obj_t *obj = resolve(target);");
        b.AppendLine("    if (obj == NULL) return;");
        b.AppendLine("    if (value)");
        b.AppendLine("        lv_obj_remove_flag(obj, LV_OBJ_FLAG_HIDDEN);");
        b.AppendLine("    else");
        b.AppendLine("        lv_obj_add_flag(obj, LV_OBJ_FLAG_HIDDEN);");
        b.AppendLine("}");
        b.AppendLine();
        b.AppendLine($"void {model.UnitName}_update_enabled({model.UnitName}_object_t target, bool value)");
        b.AppendLine("{");
        b.AppendLine("    lv_obj_t *obj = resolve(target);");
        b.AppendLine("    if (obj == NULL) return;");
        b.AppendLine("    if (value)");
        b.AppendLine("        lv_obj_remove_state(obj, LV_STATE_DISABLED);");
        b.AppendLine("    else");
        b.AppendLine("        lv_obj_add_state(obj, LV_STATE_DISABLED);");
        b.AppendLine("}");
        return b.ToString();
    }

    private static string BuildBindingCalls(ScreenContractModel model)
    {
        if (model.Events.Count == 0)
        {
            return string.Empty;
        }

        var b = new StringBuilder();
        foreach (var ev in model.Events)
        {
            b.AppendLine($"    static {model.UnitName}_event_binding_t {ev.BindingVariableName} =");
            b.AppendLine("    {");
            b.AppendLine($"        .source = {ev.ObjectEnumConstantName},");
            b.AppendLine($"        .action = {ev.ActionEnumConstantName},");
            b.AppendLine($"        .parameter_type = {ev.Parameter.TypeEnumConstantName},");
            b.AppendLine($"        .parameter = {{ {ev.Parameter.Initializer} }},");
            b.AppendLine($"        .value_type = {ev.Value.TypeEnumConstantName},");
            b.AppendLine($"        .value_source = {ev.Value.SourceEnumConstantName},");
            b.AppendLine($"        .value = {{ {ev.Value.Initializer} }}");
            b.AppendLine("    };");
            b.AppendLine($"    lv_obj_add_event_cb({ev.ObjectHandleName}, {model.UnitName}_dispatcher, {ToLvEventCodeExpression(ev.TriggerName)}, &{ev.BindingVariableName});");
        }

        return b.ToString().TrimEnd();
    }

    private LvglElementDefinition? ResolveElementDefinition(string elementName)
    {
        if (_metaModelRegistry.TryGet(elementName, out var definition) && definition is not null)
        {
            return definition;
        }

        if (_metaModelRegistry.TryGetByLvglType(elementName, out definition) && definition is not null)
        {
            return definition;
        }

        return null;
    }

    private static string? GetAttributeValue(IReadOnlyDictionary<string, string?> attributes, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (attributes.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
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

    private static string ToTypeEnum(string objectType) => objectType switch
    {
        "label" => "UI_OBJ_TYPE_LABEL",
        "led" => "UI_OBJ_TYPE_LED",
        "bar" => "UI_OBJ_TYPE_BAR",
        "slider" => "UI_OBJ_TYPE_SLIDER",
        "arc" => "UI_OBJ_TYPE_ARC",
        "spinbox" => "UI_OBJ_TYPE_SPINBOX",
        "switch" => "UI_OBJ_TYPE_SWITCH",
        "checkbox" => "UI_OBJ_TYPE_CHECKBOX",
        _ => "UI_OBJ_TYPE_NONE"
    };

    private static string ToLvEventCodeExpression(string triggerName)
        => $"LV_EVENT_{SanitizeIdentifier(triggerName, "CLICKED").ToUpperInvariant()}";

    private static string CreateActionEnumConstantName(string unitName, string actionValue)
    {
        var normalized = SanitizeIdentifier(actionValue, "ACTION_UNKNOWN").ToUpperInvariant();
        if (normalized.StartsWith("ACTION_", StringComparison.Ordinal))
        {
            normalized = normalized["ACTION_".Length..];
        }

        return $"{unitName.ToUpperInvariant()}_ACTION_{normalized}";
    }

    private static ContractParameter ResolveFixedParameter(string unitName, string objectType, string? parameterValue)
    {
        if (string.IsNullOrWhiteSpace(parameterValue))
        {
            return new ContractParameter(
                $"{unitName.ToUpperInvariant()}_PARAM_TYPE_NONE",
                ".int32_value = 0");
        }

        var trimmed = parameterValue.Trim();
        if (bool.TryParse(trimmed, out var boolValue))
        {
            return new ContractParameter(
                $"{unitName.ToUpperInvariant()}_PARAM_TYPE_BOOL",
                $".bool_value = {(boolValue ? "true" : "false")}");
        }

        if (string.Equals(objectType, "led", StringComparison.OrdinalIgnoreCase) &&
            byte.TryParse(trimmed, out var byteValue))
        {
            return new ContractParameter(
                $"{unitName.ToUpperInvariant()}_PARAM_TYPE_UINT8",
                $".uint8_value = {byteValue}");
        }

        if (int.TryParse(trimmed, out var intValue))
        {
            return new ContractParameter(
                $"{unitName.ToUpperInvariant()}_PARAM_TYPE_INT32",
                $".int32_value = {intValue}");
        }

        return new ContractParameter(
            $"{unitName.ToUpperInvariant()}_PARAM_TYPE_TEXT",
            $".text_value = {ToCString(trimmed)}");
    }

    private static ContractValue ResolveRuntimeValue(string unitName, string objectType) => objectType switch
    {
        "slider" => new ContractValue(
            $"{unitName.ToUpperInvariant()}_PARAM_TYPE_INT32",
            $"{unitName.ToUpperInvariant()}_VALUE_SOURCE_SLIDER_VALUE",
            ".int32_value = 0"),
        "bar" => new ContractValue(
            $"{unitName.ToUpperInvariant()}_PARAM_TYPE_INT32",
            $"{unitName.ToUpperInvariant()}_VALUE_SOURCE_BAR_VALUE",
            ".int32_value = 0"),
        "arc" => new ContractValue(
            $"{unitName.ToUpperInvariant()}_PARAM_TYPE_INT32",
            $"{unitName.ToUpperInvariant()}_VALUE_SOURCE_ARC_VALUE",
            ".int32_value = 0"),
        "spinbox" => new ContractValue(
            $"{unitName.ToUpperInvariant()}_PARAM_TYPE_INT32",
            $"{unitName.ToUpperInvariant()}_VALUE_SOURCE_SPINBOX_VALUE",
            ".int32_value = 0"),
        _ => new ContractValue(
            $"{unitName.ToUpperInvariant()}_PARAM_TYPE_NONE",
            $"{unitName.ToUpperInvariant()}_VALUE_SOURCE_NONE",
            ".int32_value = 0")
    };

    private static string SanitizeIdentifier(string? value, string fallback)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        normalized = Regex.Replace(normalized, @"[^A-Za-z0-9_]", "_");
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = fallback;
        }

        if (char.IsDigit(normalized[0]))
        {
            normalized = $"_{normalized}";
        }

        return normalized;
    }

    private static string ToCString(string value) =>
        $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

    private sealed record ScreenContractModel(
        string UnitName,
        string ScreenName,
        IReadOnlyList<ContractObject> Objects,
        IReadOnlyList<ContractAction> Actions,
        IReadOnlyList<ContractEvent> Events);

    private sealed record ContractObject(
        string Id,
        string HandleName,
        string EnumConstantName,
        string ObjectType,
        bool UseUpdate);

    private sealed record ContractAction(
        string Value,
        string EnumConstantName);

    private sealed record ContractParameter(
        string TypeEnumConstantName,
        string Initializer);

    private sealed record ContractValue(
        string TypeEnumConstantName,
        string SourceEnumConstantName,
        string Initializer);

    private sealed record ContractEvent(
        string ObjectId,
        string ObjectHandleName,
        string ObjectEnumConstantName,
        string ObjectType,
        string TriggerName,
        string Action,
        string ActionEnumConstantName,
        ContractParameter Parameter,
        ContractValue Value,
        string BindingVariableName);
}
