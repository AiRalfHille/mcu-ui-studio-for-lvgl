#pragma once

/* --------------------------------------------------------------------------
   GENERIERT — nicht von Hand ändern
   Quelle: Screen "portal_screen"

   Contract zwischen Display und Controller.
   Diese Datei beschreibt alle Objekte des Screens die interagieren —
   ausgehende Events (Display → Controller) und
   eingehende Updates (Controller → Display).
   -------------------------------------------------------------------------- */

#include <stdbool.h>
#include <stdint.h>
#include "lvgl.h"

typedef enum
{
    MASTERVIEW = 0,
    VIEW_LEFT,
    M1_CMD_BACKWARD,
    M1_CMD_STOP,
    M1_CMD_FORWARD,
    M1_CMD_SPEED,
    VIEW_MIDDLE,
    M1_LBL_STATUS,
    M1_LBL_PARAMETER,
    M1_LBL_SPEED,
    VIEW_RIGHT,
    UI_START_OBJ_COUNT
} ui_start_object_t;

typedef enum
{
    UI_START_ACTION_BACKWARD = 0,
    UI_START_ACTION_FORWARD,
    UI_START_ACTION_SPEED,
    UI_START_ACTION_STOP,
    UI_START_ACTION_COUNT
} ui_start_action_t;

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
    ui_start_object_t  source;
    ui_start_action_t action;
    ui_start_parameter_type_t parameter_type;
    ui_start_message_value_t parameter;
    ui_start_parameter_type_t value_type;
    ui_start_message_value_t value;
} app_message_t;

typedef struct
{
    ui_start_object_t  source;
    ui_start_action_t action;
    ui_start_parameter_type_t parameter_type;
    ui_start_message_value_t parameter;
    ui_start_parameter_type_t value_type;
    ui_start_value_source_t value_source;
    ui_start_message_value_t value;
} ui_start_event_binding_t;

void ui_start_dispatcher(lv_event_t *e);
void ui_start_update_text(ui_start_object_t target, const char *value);
void ui_start_update_value(ui_start_object_t target, int32_t value);
void ui_start_update_bool(ui_start_object_t target, bool value);
void ui_start_update_brightness(ui_start_object_t target, uint8_t value);
void ui_start_update_visible(ui_start_object_t target, bool value);
void ui_start_update_enabled(ui_start_object_t target, bool value);
