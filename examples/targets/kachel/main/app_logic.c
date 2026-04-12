#include "app_logic.h"

#include "ui_start.h"
#include "ui_start_event.h"
#include "ui_start_update.h"

#include "lvgl.h"

#include <stdio.h>
#include <string.h>

static void set_status_and_parameter(const char *status, const char *parameter)
{
    if (m1_lbl_status != NULL && status != NULL)
    {
        lv_label_set_text(m1_lbl_status, status);
    }

    if (m1_lbl_parameter != NULL && parameter != NULL)
    {
        lv_label_set_text(m1_lbl_parameter, parameter);
    }
}

static void set_speed_label_value(int32_t value)
{
    static char speed_text[16];
    snprintf(speed_text, sizeof(speed_text), "%d", (int)value);
    ui_start_set_m1_lbl_speed_text(speed_text);
}

static void set_status_from_message(const ui_start_event_message_t *message)
{
    const char *parameter_text = NULL;
    static char parameter_buffer[32];

    if (message == NULL || message->action == NULL)
    {
        return;
    }

    switch (message->parameter_type)
    {
        case UI_START_PARAM_TYPE_INT32:
            snprintf(parameter_buffer, sizeof(parameter_buffer), "%d", (int)message->parameter.int32_value);
            parameter_text = parameter_buffer;
            break;
        case UI_START_PARAM_TYPE_UINT8:
            snprintf(parameter_buffer, sizeof(parameter_buffer), "%u", (unsigned int)message->parameter.uint8_value);
            parameter_text = parameter_buffer;
            break;
        case UI_START_PARAM_TYPE_BOOL:
            parameter_text = message->parameter.bool_value ? "true" : "false";
            break;
        case UI_START_PARAM_TYPE_TEXT:
            parameter_text = message->parameter.text_value;
            break;
        case UI_START_PARAM_TYPE_NONE:
        default:
            parameter_text = "-";
            break;
    }

    if (strcmp(message->action, "ACTION_BACKWARD") == 0)
    {
        set_status_and_parameter("Rueckwaerts", parameter_text);
        return;
    }

    if (strcmp(message->action, "ACTION_FORWARD") == 0)
    {
        set_status_and_parameter("Vorwaerts", parameter_text);
        return;
    }

    if (strcmp(message->action, "ACTION_STOP") == 0)
    {
        set_status_and_parameter("Stop", parameter_text);
        return;
    }

    if (strcmp(message->action, "ACTION_SPEED") == 0)
    {
        if (message->value_type == UI_START_PARAM_TYPE_INT32)
        {
            set_speed_label_value(message->value.int32_value);
        }

        set_status_and_parameter("Speed", parameter_text);
    }
}

static bool on_ui_event(const ui_start_event_message_t *message)
{
    if (message == NULL)
    {
        return false;
    }

    set_status_from_message(message);
    return true;
}

void app_logic_init(void)
{
    set_status_and_parameter("Stop", "-");
    set_speed_label_value(0);
    ui_start_set_event_sink(on_ui_event);
}

void app_logic_tick(void)
{
    /* Intentionally empty: updates are event-driven in this example. */
}
