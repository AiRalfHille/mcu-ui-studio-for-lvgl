#pragma once

#include <stdbool.h>
#include <stdint.h>
#include "lvgl.h"

typedef enum
{
    UI_START_PARAM_TYPE_NONE = 0,
    UI_START_PARAM_TYPE_INT32,
    UI_START_PARAM_TYPE_UINT8,
    UI_START_PARAM_TYPE_BOOL,
    UI_START_PARAM_TYPE_TEXT
} ui_start_parameter_type_t;

typedef enum
{
    UI_START_VALUE_SOURCE_NONE = 0,
    UI_START_VALUE_SOURCE_SLIDER_VALUE,
    UI_START_VALUE_SOURCE_BAR_VALUE,
    UI_START_VALUE_SOURCE_ARC_VALUE,
    UI_START_VALUE_SOURCE_SPINBOX_VALUE
} ui_start_value_source_t;

typedef union
{
    int32_t     int32_value;
    uint8_t     uint8_value;
    bool        bool_value;
    const char *text_value;
} ui_start_message_value_t;

typedef struct
{
    const char *source_name;
    const char *event_group;
    const char *event_type;
    const char *action;
    ui_start_parameter_type_t parameter_type;
    ui_start_message_value_t parameter;
    ui_start_parameter_type_t value_type;
    ui_start_value_source_t value_source;
    ui_start_message_value_t value;
} ui_start_event_binding_t;

typedef struct
{
    const char *source_name;
    const char *event_group;
    const char *event_type;
    const char *action;
    ui_start_parameter_type_t parameter_type;
    ui_start_message_value_t parameter;
    ui_start_parameter_type_t value_type;
    ui_start_message_value_t value;
} ui_start_event_message_t;

typedef bool (*ui_start_event_sink_t)(ui_start_event_message_t const * message);

void ui_start_set_event_sink(ui_start_event_sink_t sink);

void ui_start_m1_dispatcher(lv_event_t * e);
void ui_start_speed_dispatcher(lv_event_t * e);
