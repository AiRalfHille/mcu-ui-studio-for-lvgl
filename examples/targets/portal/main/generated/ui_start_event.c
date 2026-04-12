/* --------------------------------------------------------------------------
   GENERIERT — nicht von Hand ändern
   Quelle: Screen "portal_screen", TemplateType = RTOS-Messages
   -------------------------------------------------------------------------- */

#include "ui_start_contract.h"
#include "message.h"

static int32_t read_runtime_value_int32(lv_event_t *e, ui_start_value_source_t value_source)
{
    lv_obj_t *obj = lv_event_get_target_obj(e);
    if (obj == NULL) return 0;

    switch (value_source)
    {
        case UI_START_VALUE_SOURCE_SLIDER_VALUE:
            return lv_slider_get_value(obj);
        case UI_START_VALUE_SOURCE_BAR_VALUE:
            return lv_bar_get_value(obj);
        case UI_START_VALUE_SOURCE_ARC_VALUE:
            return lv_arc_get_value(obj);
        case UI_START_VALUE_SOURCE_SPINBOX_VALUE:
            return lv_spinbox_get_value(obj);
        default:
            return 0;
    }
}

void ui_start_dispatcher(lv_event_t *e)
{
    if (e == NULL) return;

    ui_start_event_binding_t *binding =
        (ui_start_event_binding_t *)lv_event_get_user_data(e);

    if (binding == NULL) return;

    control_message_t msg = {
        .source_type            = CONTROL_MSG_FROM_DISPLAY,
        .payload.display.source = binding->source,
        .payload.display.action = binding->action,
        .payload.display.parameter_type = binding->parameter_type,
        .payload.display.parameter = binding->parameter,
        .payload.display.value_type = binding->value_type,
        .payload.display.value = binding->value
    };

    if (binding->value_source != UI_START_VALUE_SOURCE_NONE)
    {
        msg.payload.display.value.int32_value =
            read_runtime_value_int32(e, binding->value_source);
    }

    xQueueSend(control_queue, &msg, 0);
}
