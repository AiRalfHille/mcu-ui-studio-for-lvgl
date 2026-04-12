#pragma once

#include <stdbool.h>
#include <stdint.h>
#include "lvgl.h"

typedef enum
{
    UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_BACKGROUND_COLOR,
    UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_ENABLED,
    UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_FONT,
    UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_TEXT,
    UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_TEXT_COLOR,
    UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_VISIBLE,
    UI_START_UPDATE_TARGET_M1_CMD_FORWARD_BACKGROUND_COLOR,
    UI_START_UPDATE_TARGET_M1_CMD_FORWARD_ENABLED,
    UI_START_UPDATE_TARGET_M1_CMD_FORWARD_FONT,
    UI_START_UPDATE_TARGET_M1_CMD_FORWARD_TEXT,
    UI_START_UPDATE_TARGET_M1_CMD_FORWARD_TEXT_COLOR,
    UI_START_UPDATE_TARGET_M1_CMD_FORWARD_VISIBLE,
    UI_START_UPDATE_TARGET_M1_CMD_SPEED_BACKGROUND_COLOR,
    UI_START_UPDATE_TARGET_M1_CMD_SPEED_ENABLED,
    UI_START_UPDATE_TARGET_M1_CMD_SPEED_FONT,
    UI_START_UPDATE_TARGET_M1_CMD_SPEED_TEXT_COLOR,
    UI_START_UPDATE_TARGET_M1_CMD_SPEED_VALUE,
    UI_START_UPDATE_TARGET_M1_CMD_SPEED_VISIBLE,
    UI_START_UPDATE_TARGET_M1_CMD_STOP_BACKGROUND_COLOR,
    UI_START_UPDATE_TARGET_M1_CMD_STOP_ENABLED,
    UI_START_UPDATE_TARGET_M1_CMD_STOP_FONT,
    UI_START_UPDATE_TARGET_M1_CMD_STOP_TEXT,
    UI_START_UPDATE_TARGET_M1_CMD_STOP_TEXT_COLOR,
    UI_START_UPDATE_TARGET_M1_CMD_STOP_VISIBLE,
    UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_BACKGROUND_COLOR,
    UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_ENABLED,
    UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_FONT,
    UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_TEXT,
    UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_TEXT_COLOR,
    UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_VISIBLE,
    UI_START_UPDATE_TARGET_M1_LBL_SPEED_BACKGROUND_COLOR,
    UI_START_UPDATE_TARGET_M1_LBL_SPEED_ENABLED,
    UI_START_UPDATE_TARGET_M1_LBL_SPEED_FONT,
    UI_START_UPDATE_TARGET_M1_LBL_SPEED_TEXT,
    UI_START_UPDATE_TARGET_M1_LBL_SPEED_TEXT_COLOR,
    UI_START_UPDATE_TARGET_M1_LBL_SPEED_VISIBLE,
    UI_START_UPDATE_TARGET_M1_LBL_STATUS_BACKGROUND_COLOR,
    UI_START_UPDATE_TARGET_M1_LBL_STATUS_ENABLED,
    UI_START_UPDATE_TARGET_M1_LBL_STATUS_FONT,
    UI_START_UPDATE_TARGET_M1_LBL_STATUS_TEXT,
    UI_START_UPDATE_TARGET_M1_LBL_STATUS_TEXT_COLOR,
    UI_START_UPDATE_TARGET_M1_LBL_STATUS_VISIBLE
} ui_start_update_target_t;

typedef enum
{
    UI_START_UPDATE_VALUE_TEXT,
    UI_START_UPDATE_VALUE_INT32,
    UI_START_UPDATE_VALUE_BOOL,
    UI_START_UPDATE_VALUE_COLOR,
    UI_START_UPDATE_VALUE_FONT
} ui_start_update_value_type_t;

typedef struct
{
    ui_start_update_value_type_t type;
    union
    {
        const char *text;
        int32_t int32_value;
        bool bool_value;
        lv_color_t color_value;
        const lv_font_t *font_value;
    } value;
} ui_start_update_value_t;

typedef struct
{
    ui_start_update_target_t target;
    ui_start_update_value_t payload;
} ui_start_update_message_t;

void ui_start_apply_update(const ui_start_update_message_t * message);
void ui_start_update_text(ui_start_update_target_t target, const char * value);
void ui_start_update_value(ui_start_update_target_t target, int32_t value);
void ui_start_update_checked(ui_start_update_target_t target, bool value);
void ui_start_update_visible(ui_start_update_target_t target, bool value);
void ui_start_update_enabled(ui_start_update_target_t target, bool value);
void ui_start_update_text_color(ui_start_update_target_t target, lv_color_t value);
void ui_start_update_background_color(ui_start_update_target_t target, lv_color_t value);
void ui_start_update_font(ui_start_update_target_t target, const lv_font_t * value);

void ui_start_set_m1_cmd_backward_text(const char * value);
void ui_start_set_m1_cmd_backward_visible(bool value);
void ui_start_set_m1_cmd_backward_enabled(bool value);
void ui_start_set_m1_cmd_backward_text_color(lv_color_t value);
void ui_start_set_m1_cmd_backward_background_color(lv_color_t value);
void ui_start_set_m1_cmd_backward_font(const lv_font_t * value);
void ui_start_set_m1_cmd_forward_text(const char * value);
void ui_start_set_m1_cmd_forward_visible(bool value);
void ui_start_set_m1_cmd_forward_enabled(bool value);
void ui_start_set_m1_cmd_forward_text_color(lv_color_t value);
void ui_start_set_m1_cmd_forward_background_color(lv_color_t value);
void ui_start_set_m1_cmd_forward_font(const lv_font_t * value);
void ui_start_set_m1_cmd_speed_value(int32_t value);
void ui_start_set_m1_cmd_speed_visible(bool value);
void ui_start_set_m1_cmd_speed_enabled(bool value);
void ui_start_set_m1_cmd_speed_text_color(lv_color_t value);
void ui_start_set_m1_cmd_speed_background_color(lv_color_t value);
void ui_start_set_m1_cmd_speed_font(const lv_font_t * value);
void ui_start_set_m1_cmd_stop_text(const char * value);
void ui_start_set_m1_cmd_stop_visible(bool value);
void ui_start_set_m1_cmd_stop_enabled(bool value);
void ui_start_set_m1_cmd_stop_text_color(lv_color_t value);
void ui_start_set_m1_cmd_stop_background_color(lv_color_t value);
void ui_start_set_m1_cmd_stop_font(const lv_font_t * value);
void ui_start_set_m1_lbl_parameter_text(const char * value);
void ui_start_set_m1_lbl_parameter_visible(bool value);
void ui_start_set_m1_lbl_parameter_enabled(bool value);
void ui_start_set_m1_lbl_parameter_text_color(lv_color_t value);
void ui_start_set_m1_lbl_parameter_background_color(lv_color_t value);
void ui_start_set_m1_lbl_parameter_font(const lv_font_t * value);
void ui_start_set_m1_lbl_speed_text(const char * value);
void ui_start_set_m1_lbl_speed_visible(bool value);
void ui_start_set_m1_lbl_speed_enabled(bool value);
void ui_start_set_m1_lbl_speed_text_color(lv_color_t value);
void ui_start_set_m1_lbl_speed_background_color(lv_color_t value);
void ui_start_set_m1_lbl_speed_font(const lv_font_t * value);
void ui_start_set_m1_lbl_status_text(const char * value);
void ui_start_set_m1_lbl_status_visible(bool value);
void ui_start_set_m1_lbl_status_enabled(bool value);
void ui_start_set_m1_lbl_status_text_color(lv_color_t value);
void ui_start_set_m1_lbl_status_background_color(lv_color_t value);
void ui_start_set_m1_lbl_status_font(const lv_font_t * value);
