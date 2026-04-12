/* --------------------------------------------------------------------------
   GENERIERT — nicht von Hand ändern
   Quelle: Screen "portal_screen", TemplateType = RTOS-Messages
   -------------------------------------------------------------------------- */

#include "ui_start_contract.h"
#include "ui_start.h"

typedef enum
{
    UI_OBJ_TYPE_NONE = 0,
    UI_OBJ_TYPE_LABEL,
    UI_OBJ_TYPE_LED,
    UI_OBJ_TYPE_BAR,
    UI_OBJ_TYPE_SLIDER,
    UI_OBJ_TYPE_ARC,
    UI_OBJ_TYPE_SPINBOX,
    UI_OBJ_TYPE_SWITCH,
    UI_OBJ_TYPE_CHECKBOX,
} ui_obj_type_t;

static lv_obj_t **ui_start_objects[UI_START_OBJ_COUNT] = {
    [M1_CMD_BACKWARD] = &m1_cmd_backward,
    [M1_CMD_STOP] = &m1_cmd_stop,
    [M1_CMD_FORWARD] = &m1_cmd_forward,
    [M1_CMD_SPEED] = &m1_cmd_speed,
    [M1_LBL_STATUS] = &m1_lbl_status,
    [M1_LBL_PARAMETER] = &m1_lbl_parameter,
    [M1_LBL_SPEED] = &m1_lbl_speed,
};

static const ui_obj_type_t ui_start_object_types[UI_START_OBJ_COUNT] = {
    [M1_CMD_BACKWARD] = UI_OBJ_TYPE_NONE,
    [M1_CMD_STOP] = UI_OBJ_TYPE_NONE,
    [M1_CMD_FORWARD] = UI_OBJ_TYPE_NONE,
    [M1_CMD_SPEED] = UI_OBJ_TYPE_SLIDER,
    [M1_LBL_STATUS] = UI_OBJ_TYPE_LABEL,
    [M1_LBL_PARAMETER] = UI_OBJ_TYPE_LABEL,
    [M1_LBL_SPEED] = UI_OBJ_TYPE_LABEL,
};

static lv_obj_t *resolve(ui_start_object_t target)
{
    if (target >= UI_START_OBJ_COUNT) return NULL;
    return ui_start_objects[target] ? *ui_start_objects[target] : NULL;
}

void ui_start_update_text(ui_start_object_t target, const char *value)
{
    if (value == NULL) return;
    lv_obj_t *obj = resolve(target);
    if (obj == NULL) return;
    if (ui_start_object_types[target] == UI_OBJ_TYPE_LABEL)
        lv_label_set_text(obj, value);
}

void ui_start_update_value(ui_start_object_t target, int32_t value)
{
    lv_obj_t *obj = resolve(target);
    if (obj == NULL) return;
    switch (ui_start_object_types[target])
    {
        case UI_OBJ_TYPE_BAR:    lv_bar_set_value(obj, value, LV_ANIM_OFF); break;
        case UI_OBJ_TYPE_SLIDER: lv_slider_set_value(obj, value, LV_ANIM_OFF); break;
        case UI_OBJ_TYPE_ARC:    lv_arc_set_value(obj, value); break;
        case UI_OBJ_TYPE_SPINBOX: lv_spinbox_set_value(obj, value); break;
        default: break;
    }
}

void ui_start_update_bool(ui_start_object_t target, bool value)
{
    lv_obj_t *obj = resolve(target);
    if (obj == NULL) return;
    switch (ui_start_object_types[target])
    {
        case UI_OBJ_TYPE_LED:
            if (value) lv_led_on(obj); else lv_led_off(obj);
            break;
        case UI_OBJ_TYPE_SWITCH:
        case UI_OBJ_TYPE_CHECKBOX:
            if (value) lv_obj_add_state(obj, LV_STATE_CHECKED);
            else       lv_obj_remove_state(obj, LV_STATE_CHECKED);
            break;
        default: break;
    }
}

void ui_start_update_brightness(ui_start_object_t target, uint8_t value)
{
    lv_obj_t *obj = resolve(target);
    if (obj == NULL) return;
    if (ui_start_object_types[target] == UI_OBJ_TYPE_LED)
        lv_led_set_brightness(obj, value);
}

void ui_start_update_visible(ui_start_object_t target, bool value)
{
    lv_obj_t *obj = resolve(target);
    if (obj == NULL) return;
    if (value)
        lv_obj_remove_flag(obj, LV_OBJ_FLAG_HIDDEN);
    else
        lv_obj_add_flag(obj, LV_OBJ_FLAG_HIDDEN);
}

void ui_start_update_enabled(ui_start_object_t target, bool value)
{
    lv_obj_t *obj = resolve(target);
    if (obj == NULL) return;
    if (value)
        lv_obj_remove_state(obj, LV_STATE_DISABLED);
    else
        lv_obj_add_state(obj, LV_STATE_DISABLED);
}
