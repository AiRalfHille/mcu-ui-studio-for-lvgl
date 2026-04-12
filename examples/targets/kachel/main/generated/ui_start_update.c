#include "ui_start_update.h"
#include "ui_start.h"

void ui_start_update_text(ui_start_update_target_t target, const char * value)
{
    if(value == NULL)
    {
        return;
    }

    switch(target)
    {
        case UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_TEXT:
        {
            if(m1_cmd_backward == NULL)
            {
                return;
            }
            lv_obj_t * label = lv_obj_get_child(m1_cmd_backward, 0);
            if(label == NULL)
            {
                return;
            }
            lv_label_set_text(label, value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_FORWARD_TEXT:
        {
            if(m1_cmd_forward == NULL)
            {
                return;
            }
            lv_obj_t * label = lv_obj_get_child(m1_cmd_forward, 0);
            if(label == NULL)
            {
                return;
            }
            lv_label_set_text(label, value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_STOP_TEXT:
        {
            if(m1_cmd_stop == NULL)
            {
                return;
            }
            lv_obj_t * label = lv_obj_get_child(m1_cmd_stop, 0);
            if(label == NULL)
            {
                return;
            }
            lv_label_set_text(label, value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_TEXT:
        {
            if(m1_lbl_parameter == NULL)
            {
                return;
            }
            lv_label_set_text(m1_lbl_parameter, value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_SPEED_TEXT:
        {
            if(m1_lbl_speed == NULL)
            {
                return;
            }
            lv_label_set_text(m1_lbl_speed, value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_STATUS_TEXT:
        {
            if(m1_lbl_status == NULL)
            {
                return;
            }
            lv_label_set_text(m1_lbl_status, value);
            return;
        }
        default:
            return;
    }
}

void ui_start_update_value(ui_start_update_target_t target, int32_t value)
{
    switch(target)
    {
        case UI_START_UPDATE_TARGET_M1_CMD_SPEED_VALUE:
        {
            if(m1_cmd_speed == NULL)
            {
                return;
            }
            lv_slider_set_value(m1_cmd_speed, value, LV_ANIM_OFF);
            return;
        }
        default:
            return;
    }
}

void ui_start_update_checked(ui_start_update_target_t target, bool value)
{
    switch(target)
    {
        default:
            return;
    }
}

void ui_start_update_visible(ui_start_update_target_t target, bool value)
{
    switch(target)
    {
        case UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_VISIBLE:
        {
            if(m1_cmd_backward == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_flag(m1_cmd_backward, LV_OBJ_FLAG_HIDDEN);
            }
            else
            {
                lv_obj_add_flag(m1_cmd_backward, LV_OBJ_FLAG_HIDDEN);
            }
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_FORWARD_VISIBLE:
        {
            if(m1_cmd_forward == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_flag(m1_cmd_forward, LV_OBJ_FLAG_HIDDEN);
            }
            else
            {
                lv_obj_add_flag(m1_cmd_forward, LV_OBJ_FLAG_HIDDEN);
            }
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_SPEED_VISIBLE:
        {
            if(m1_cmd_speed == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_flag(m1_cmd_speed, LV_OBJ_FLAG_HIDDEN);
            }
            else
            {
                lv_obj_add_flag(m1_cmd_speed, LV_OBJ_FLAG_HIDDEN);
            }
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_STOP_VISIBLE:
        {
            if(m1_cmd_stop == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_flag(m1_cmd_stop, LV_OBJ_FLAG_HIDDEN);
            }
            else
            {
                lv_obj_add_flag(m1_cmd_stop, LV_OBJ_FLAG_HIDDEN);
            }
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_VISIBLE:
        {
            if(m1_lbl_parameter == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_flag(m1_lbl_parameter, LV_OBJ_FLAG_HIDDEN);
            }
            else
            {
                lv_obj_add_flag(m1_lbl_parameter, LV_OBJ_FLAG_HIDDEN);
            }
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_SPEED_VISIBLE:
        {
            if(m1_lbl_speed == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_flag(m1_lbl_speed, LV_OBJ_FLAG_HIDDEN);
            }
            else
            {
                lv_obj_add_flag(m1_lbl_speed, LV_OBJ_FLAG_HIDDEN);
            }
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_STATUS_VISIBLE:
        {
            if(m1_lbl_status == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_flag(m1_lbl_status, LV_OBJ_FLAG_HIDDEN);
            }
            else
            {
                lv_obj_add_flag(m1_lbl_status, LV_OBJ_FLAG_HIDDEN);
            }
            return;
        }
        default:
            return;
    }
}

void ui_start_update_enabled(ui_start_update_target_t target, bool value)
{
    switch(target)
    {
        case UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_ENABLED:
        {
            if(m1_cmd_backward == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_state(m1_cmd_backward, LV_STATE_DISABLED);
            }
            else
            {
                lv_obj_add_state(m1_cmd_backward, LV_STATE_DISABLED);
            }
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_FORWARD_ENABLED:
        {
            if(m1_cmd_forward == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_state(m1_cmd_forward, LV_STATE_DISABLED);
            }
            else
            {
                lv_obj_add_state(m1_cmd_forward, LV_STATE_DISABLED);
            }
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_SPEED_ENABLED:
        {
            if(m1_cmd_speed == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_state(m1_cmd_speed, LV_STATE_DISABLED);
            }
            else
            {
                lv_obj_add_state(m1_cmd_speed, LV_STATE_DISABLED);
            }
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_STOP_ENABLED:
        {
            if(m1_cmd_stop == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_state(m1_cmd_stop, LV_STATE_DISABLED);
            }
            else
            {
                lv_obj_add_state(m1_cmd_stop, LV_STATE_DISABLED);
            }
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_ENABLED:
        {
            if(m1_lbl_parameter == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_state(m1_lbl_parameter, LV_STATE_DISABLED);
            }
            else
            {
                lv_obj_add_state(m1_lbl_parameter, LV_STATE_DISABLED);
            }
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_SPEED_ENABLED:
        {
            if(m1_lbl_speed == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_state(m1_lbl_speed, LV_STATE_DISABLED);
            }
            else
            {
                lv_obj_add_state(m1_lbl_speed, LV_STATE_DISABLED);
            }
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_STATUS_ENABLED:
        {
            if(m1_lbl_status == NULL)
            {
                return;
            }
            if(value)
            {
                lv_obj_remove_state(m1_lbl_status, LV_STATE_DISABLED);
            }
            else
            {
                lv_obj_add_state(m1_lbl_status, LV_STATE_DISABLED);
            }
            return;
        }
        default:
            return;
    }
}

void ui_start_update_text_color(ui_start_update_target_t target, lv_color_t value)
{
    switch(target)
    {
        case UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_TEXT_COLOR:
        {
            if(m1_cmd_backward == NULL)
            {
                return;
            }
            lv_obj_set_style_text_color(m1_cmd_backward, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_FORWARD_TEXT_COLOR:
        {
            if(m1_cmd_forward == NULL)
            {
                return;
            }
            lv_obj_set_style_text_color(m1_cmd_forward, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_SPEED_TEXT_COLOR:
        {
            if(m1_cmd_speed == NULL)
            {
                return;
            }
            lv_obj_set_style_text_color(m1_cmd_speed, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_STOP_TEXT_COLOR:
        {
            if(m1_cmd_stop == NULL)
            {
                return;
            }
            lv_obj_set_style_text_color(m1_cmd_stop, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_TEXT_COLOR:
        {
            if(m1_lbl_parameter == NULL)
            {
                return;
            }
            lv_obj_set_style_text_color(m1_lbl_parameter, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_SPEED_TEXT_COLOR:
        {
            if(m1_lbl_speed == NULL)
            {
                return;
            }
            lv_obj_set_style_text_color(m1_lbl_speed, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_STATUS_TEXT_COLOR:
        {
            if(m1_lbl_status == NULL)
            {
                return;
            }
            lv_obj_set_style_text_color(m1_lbl_status, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        default:
            return;
    }
}

void ui_start_update_background_color(ui_start_update_target_t target, lv_color_t value)
{
    switch(target)
    {
        case UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_BACKGROUND_COLOR:
        {
            if(m1_cmd_backward == NULL)
            {
                return;
            }
            lv_obj_set_style_bg_color(m1_cmd_backward, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_FORWARD_BACKGROUND_COLOR:
        {
            if(m1_cmd_forward == NULL)
            {
                return;
            }
            lv_obj_set_style_bg_color(m1_cmd_forward, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_SPEED_BACKGROUND_COLOR:
        {
            if(m1_cmd_speed == NULL)
            {
                return;
            }
            lv_obj_set_style_bg_color(m1_cmd_speed, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_STOP_BACKGROUND_COLOR:
        {
            if(m1_cmd_stop == NULL)
            {
                return;
            }
            lv_obj_set_style_bg_color(m1_cmd_stop, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_BACKGROUND_COLOR:
        {
            if(m1_lbl_parameter == NULL)
            {
                return;
            }
            lv_obj_set_style_bg_color(m1_lbl_parameter, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_SPEED_BACKGROUND_COLOR:
        {
            if(m1_lbl_speed == NULL)
            {
                return;
            }
            lv_obj_set_style_bg_color(m1_lbl_speed, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_STATUS_BACKGROUND_COLOR:
        {
            if(m1_lbl_status == NULL)
            {
                return;
            }
            lv_obj_set_style_bg_color(m1_lbl_status, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        default:
            return;
    }
}

void ui_start_update_font(ui_start_update_target_t target, const lv_font_t * value)
{
    if(value == NULL)
    {
        return;
    }

    switch(target)
    {
        case UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_FONT:
        {
            if(m1_cmd_backward == NULL)
            {
                return;
            }
            lv_obj_set_style_text_font(m1_cmd_backward, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_FORWARD_FONT:
        {
            if(m1_cmd_forward == NULL)
            {
                return;
            }
            lv_obj_set_style_text_font(m1_cmd_forward, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_SPEED_FONT:
        {
            if(m1_cmd_speed == NULL)
            {
                return;
            }
            lv_obj_set_style_text_font(m1_cmd_speed, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_STOP_FONT:
        {
            if(m1_cmd_stop == NULL)
            {
                return;
            }
            lv_obj_set_style_text_font(m1_cmd_stop, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_FONT:
        {
            if(m1_lbl_parameter == NULL)
            {
                return;
            }
            lv_obj_set_style_text_font(m1_lbl_parameter, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_SPEED_FONT:
        {
            if(m1_lbl_speed == NULL)
            {
                return;
            }
            lv_obj_set_style_text_font(m1_lbl_speed, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_STATUS_FONT:
        {
            if(m1_lbl_status == NULL)
            {
                return;
            }
            lv_obj_set_style_text_font(m1_lbl_status, value, LV_PART_MAIN | LV_STATE_DEFAULT);
            return;
        }
        default:
            return;
    }
}

void ui_start_apply_update(const ui_start_update_message_t * message)
{
    if(message == NULL)
    {
        return;
    }

    switch(message->target)
    {
        case UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_TEXT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_TEXT)
            {
                return;
            }
            ui_start_update_text(message->target, message->payload.value.text);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_VISIBLE:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_visible(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_ENABLED:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_enabled(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_TEXT_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_text_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_BACKGROUND_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_background_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_FONT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_FONT)
            {
                return;
            }
            ui_start_update_font(message->target, message->payload.value.font_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_FORWARD_TEXT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_TEXT)
            {
                return;
            }
            ui_start_update_text(message->target, message->payload.value.text);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_FORWARD_VISIBLE:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_visible(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_FORWARD_ENABLED:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_enabled(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_FORWARD_TEXT_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_text_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_FORWARD_BACKGROUND_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_background_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_FORWARD_FONT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_FONT)
            {
                return;
            }
            ui_start_update_font(message->target, message->payload.value.font_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_SPEED_VALUE:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_INT32)
            {
                return;
            }
            ui_start_update_value(message->target, message->payload.value.int32_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_SPEED_VISIBLE:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_visible(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_SPEED_ENABLED:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_enabled(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_SPEED_TEXT_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_text_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_SPEED_BACKGROUND_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_background_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_SPEED_FONT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_FONT)
            {
                return;
            }
            ui_start_update_font(message->target, message->payload.value.font_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_STOP_TEXT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_TEXT)
            {
                return;
            }
            ui_start_update_text(message->target, message->payload.value.text);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_STOP_VISIBLE:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_visible(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_STOP_ENABLED:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_enabled(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_STOP_TEXT_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_text_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_STOP_BACKGROUND_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_background_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_CMD_STOP_FONT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_FONT)
            {
                return;
            }
            ui_start_update_font(message->target, message->payload.value.font_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_TEXT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_TEXT)
            {
                return;
            }
            ui_start_update_text(message->target, message->payload.value.text);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_VISIBLE:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_visible(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_ENABLED:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_enabled(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_TEXT_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_text_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_BACKGROUND_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_background_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_FONT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_FONT)
            {
                return;
            }
            ui_start_update_font(message->target, message->payload.value.font_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_SPEED_TEXT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_TEXT)
            {
                return;
            }
            ui_start_update_text(message->target, message->payload.value.text);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_SPEED_VISIBLE:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_visible(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_SPEED_ENABLED:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_enabled(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_SPEED_TEXT_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_text_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_SPEED_BACKGROUND_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_background_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_SPEED_FONT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_FONT)
            {
                return;
            }
            ui_start_update_font(message->target, message->payload.value.font_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_STATUS_TEXT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_TEXT)
            {
                return;
            }
            ui_start_update_text(message->target, message->payload.value.text);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_STATUS_VISIBLE:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_visible(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_STATUS_ENABLED:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_BOOL)
            {
                return;
            }
            ui_start_update_enabled(message->target, message->payload.value.bool_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_STATUS_TEXT_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_text_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_STATUS_BACKGROUND_COLOR:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_COLOR)
            {
                return;
            }
            ui_start_update_background_color(message->target, message->payload.value.color_value);
            return;
        }
        case UI_START_UPDATE_TARGET_M1_LBL_STATUS_FONT:
        {
            if(message->payload.type != UI_START_UPDATE_VALUE_FONT)
            {
                return;
            }
            ui_start_update_font(message->target, message->payload.value.font_value);
            return;
        }
        default:
            return;
    }
}

void ui_start_set_m1_cmd_backward_text(const char * value)
{
    ui_start_update_text(UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_TEXT, value);
}

void ui_start_set_m1_cmd_backward_visible(bool value)
{
    ui_start_update_visible(UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_VISIBLE, value);
}

void ui_start_set_m1_cmd_backward_enabled(bool value)
{
    ui_start_update_enabled(UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_ENABLED, value);
}

void ui_start_set_m1_cmd_backward_text_color(lv_color_t value)
{
    ui_start_update_text_color(UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_TEXT_COLOR, value);
}

void ui_start_set_m1_cmd_backward_background_color(lv_color_t value)
{
    ui_start_update_background_color(UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_BACKGROUND_COLOR, value);
}

void ui_start_set_m1_cmd_backward_font(const lv_font_t * value)
{
    ui_start_update_font(UI_START_UPDATE_TARGET_M1_CMD_BACKWARD_FONT, value);
}

void ui_start_set_m1_cmd_forward_text(const char * value)
{
    ui_start_update_text(UI_START_UPDATE_TARGET_M1_CMD_FORWARD_TEXT, value);
}

void ui_start_set_m1_cmd_forward_visible(bool value)
{
    ui_start_update_visible(UI_START_UPDATE_TARGET_M1_CMD_FORWARD_VISIBLE, value);
}

void ui_start_set_m1_cmd_forward_enabled(bool value)
{
    ui_start_update_enabled(UI_START_UPDATE_TARGET_M1_CMD_FORWARD_ENABLED, value);
}

void ui_start_set_m1_cmd_forward_text_color(lv_color_t value)
{
    ui_start_update_text_color(UI_START_UPDATE_TARGET_M1_CMD_FORWARD_TEXT_COLOR, value);
}

void ui_start_set_m1_cmd_forward_background_color(lv_color_t value)
{
    ui_start_update_background_color(UI_START_UPDATE_TARGET_M1_CMD_FORWARD_BACKGROUND_COLOR, value);
}

void ui_start_set_m1_cmd_forward_font(const lv_font_t * value)
{
    ui_start_update_font(UI_START_UPDATE_TARGET_M1_CMD_FORWARD_FONT, value);
}

void ui_start_set_m1_cmd_speed_value(int32_t value)
{
    ui_start_update_value(UI_START_UPDATE_TARGET_M1_CMD_SPEED_VALUE, value);
}

void ui_start_set_m1_cmd_speed_visible(bool value)
{
    ui_start_update_visible(UI_START_UPDATE_TARGET_M1_CMD_SPEED_VISIBLE, value);
}

void ui_start_set_m1_cmd_speed_enabled(bool value)
{
    ui_start_update_enabled(UI_START_UPDATE_TARGET_M1_CMD_SPEED_ENABLED, value);
}

void ui_start_set_m1_cmd_speed_text_color(lv_color_t value)
{
    ui_start_update_text_color(UI_START_UPDATE_TARGET_M1_CMD_SPEED_TEXT_COLOR, value);
}

void ui_start_set_m1_cmd_speed_background_color(lv_color_t value)
{
    ui_start_update_background_color(UI_START_UPDATE_TARGET_M1_CMD_SPEED_BACKGROUND_COLOR, value);
}

void ui_start_set_m1_cmd_speed_font(const lv_font_t * value)
{
    ui_start_update_font(UI_START_UPDATE_TARGET_M1_CMD_SPEED_FONT, value);
}

void ui_start_set_m1_cmd_stop_text(const char * value)
{
    ui_start_update_text(UI_START_UPDATE_TARGET_M1_CMD_STOP_TEXT, value);
}

void ui_start_set_m1_cmd_stop_visible(bool value)
{
    ui_start_update_visible(UI_START_UPDATE_TARGET_M1_CMD_STOP_VISIBLE, value);
}

void ui_start_set_m1_cmd_stop_enabled(bool value)
{
    ui_start_update_enabled(UI_START_UPDATE_TARGET_M1_CMD_STOP_ENABLED, value);
}

void ui_start_set_m1_cmd_stop_text_color(lv_color_t value)
{
    ui_start_update_text_color(UI_START_UPDATE_TARGET_M1_CMD_STOP_TEXT_COLOR, value);
}

void ui_start_set_m1_cmd_stop_background_color(lv_color_t value)
{
    ui_start_update_background_color(UI_START_UPDATE_TARGET_M1_CMD_STOP_BACKGROUND_COLOR, value);
}

void ui_start_set_m1_cmd_stop_font(const lv_font_t * value)
{
    ui_start_update_font(UI_START_UPDATE_TARGET_M1_CMD_STOP_FONT, value);
}

void ui_start_set_m1_lbl_parameter_text(const char * value)
{
    ui_start_update_text(UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_TEXT, value);
}

void ui_start_set_m1_lbl_parameter_visible(bool value)
{
    ui_start_update_visible(UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_VISIBLE, value);
}

void ui_start_set_m1_lbl_parameter_enabled(bool value)
{
    ui_start_update_enabled(UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_ENABLED, value);
}

void ui_start_set_m1_lbl_parameter_text_color(lv_color_t value)
{
    ui_start_update_text_color(UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_TEXT_COLOR, value);
}

void ui_start_set_m1_lbl_parameter_background_color(lv_color_t value)
{
    ui_start_update_background_color(UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_BACKGROUND_COLOR, value);
}

void ui_start_set_m1_lbl_parameter_font(const lv_font_t * value)
{
    ui_start_update_font(UI_START_UPDATE_TARGET_M1_LBL_PARAMETER_FONT, value);
}

void ui_start_set_m1_lbl_speed_text(const char * value)
{
    ui_start_update_text(UI_START_UPDATE_TARGET_M1_LBL_SPEED_TEXT, value);
}

void ui_start_set_m1_lbl_speed_visible(bool value)
{
    ui_start_update_visible(UI_START_UPDATE_TARGET_M1_LBL_SPEED_VISIBLE, value);
}

void ui_start_set_m1_lbl_speed_enabled(bool value)
{
    ui_start_update_enabled(UI_START_UPDATE_TARGET_M1_LBL_SPEED_ENABLED, value);
}

void ui_start_set_m1_lbl_speed_text_color(lv_color_t value)
{
    ui_start_update_text_color(UI_START_UPDATE_TARGET_M1_LBL_SPEED_TEXT_COLOR, value);
}

void ui_start_set_m1_lbl_speed_background_color(lv_color_t value)
{
    ui_start_update_background_color(UI_START_UPDATE_TARGET_M1_LBL_SPEED_BACKGROUND_COLOR, value);
}

void ui_start_set_m1_lbl_speed_font(const lv_font_t * value)
{
    ui_start_update_font(UI_START_UPDATE_TARGET_M1_LBL_SPEED_FONT, value);
}

void ui_start_set_m1_lbl_status_text(const char * value)
{
    ui_start_update_text(UI_START_UPDATE_TARGET_M1_LBL_STATUS_TEXT, value);
}

void ui_start_set_m1_lbl_status_visible(bool value)
{
    ui_start_update_visible(UI_START_UPDATE_TARGET_M1_LBL_STATUS_VISIBLE, value);
}

void ui_start_set_m1_lbl_status_enabled(bool value)
{
    ui_start_update_enabled(UI_START_UPDATE_TARGET_M1_LBL_STATUS_ENABLED, value);
}

void ui_start_set_m1_lbl_status_text_color(lv_color_t value)
{
    ui_start_update_text_color(UI_START_UPDATE_TARGET_M1_LBL_STATUS_TEXT_COLOR, value);
}

void ui_start_set_m1_lbl_status_background_color(lv_color_t value)
{
    ui_start_update_background_color(UI_START_UPDATE_TARGET_M1_LBL_STATUS_BACKGROUND_COLOR, value);
}

void ui_start_set_m1_lbl_status_font(const lv_font_t * value)
{
    ui_start_update_font(UI_START_UPDATE_TARGET_M1_LBL_STATUS_FONT, value);
}

