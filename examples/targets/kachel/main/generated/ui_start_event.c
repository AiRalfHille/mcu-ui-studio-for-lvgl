#include "ui_start_event.h"
#include <string.h>

static ui_start_event_sink_t s_event_sink = NULL;

void ui_start_set_event_sink(ui_start_event_sink_t sink)
{
    s_event_sink = sink;
}

static int32_t read_runtime_value_int32(lv_event_t * e, ui_start_value_source_t value_source)
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

static void dispatch_ui_event(lv_event_t * e, const ui_start_event_binding_t * binding)
{
    if(binding == NULL)
    {
        return;
    }

    if(s_event_sink == NULL)
    {
        return;
    }

    ui_start_event_message_t msg =
    {
        .source_name = binding->source_name,
        .event_group = binding->event_group,
        .event_type = binding->event_type,
        .action = binding->action,
        .parameter_type = binding->parameter_type,
        .parameter = binding->parameter,
        .value_type = binding->value_type,
        .value = binding->value
    };

    if (binding->value_source != UI_START_VALUE_SOURCE_NONE)
    {
        msg.value.int32_value = read_runtime_value_int32(e, binding->value_source);
    }

    (void)s_event_sink(&msg);
}

void ui_start_m1_dispatcher(lv_event_t * e)
{
    if(e == NULL)
    {
        return;
    }

    ui_start_event_binding_t * binding = (ui_start_event_binding_t *)lv_event_get_user_data(e);
    if(binding == NULL)
    {
        return;
    }

    switch(lv_event_get_code(e))
    {
        case LV_EVENT_CLICKED:
        {
            if(strcmp(binding->event_type, "backward") == 0)
            {
                if(strcmp(binding->source_name, "m1_cmd_backward") == 0)
                {
                    dispatch_ui_event(e, binding);
                    return;
                }
            }
            if(strcmp(binding->event_type, "forward") == 0)
            {
                if(strcmp(binding->source_name, "m1_cmd_forward") == 0)
                {
                    dispatch_ui_event(e, binding);
                    return;
                }
            }
            if(strcmp(binding->event_type, "stop") == 0)
            {
                if(strcmp(binding->source_name, "m1_cmd_stop") == 0)
                {
                    dispatch_ui_event(e, binding);
                    return;
                }
            }
            break;
        }
        default:
            break;
    }
}

void ui_start_speed_dispatcher(lv_event_t * e)
{
    if(e == NULL)
    {
        return;
    }

    ui_start_event_binding_t * binding = (ui_start_event_binding_t *)lv_event_get_user_data(e);
    if(binding == NULL)
    {
        return;
    }

    switch(lv_event_get_code(e))
    {
        case LV_EVENT_RELEASED:
        {
            if(strcmp(binding->source_name, "m1_cmd_speed") == 0)
            {
                dispatch_ui_event(e, binding);
                return;
            }
            break;
        }
        default:
            break;
    }
}

