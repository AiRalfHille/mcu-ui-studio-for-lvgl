#include "ui_start.h"
#include "ui_start_contract.h"
#include "lvgl.h"

/* weitere Includes nach Bedarf */

lv_obj_t * m1_cmd_backward = NULL;
lv_obj_t * m1_cmd_forward = NULL;
lv_obj_t * m1_cmd_speed = NULL;
lv_obj_t * m1_cmd_stop = NULL;
lv_obj_t * m1_lbl_parameter = NULL;
lv_obj_t * m1_lbl_speed = NULL;
lv_obj_t * m1_lbl_status = NULL;
lv_obj_t * masterview = NULL;
lv_obj_t * view_left = NULL;
lv_obj_t * view_middle = NULL;
lv_obj_t * view_right = NULL;

static void create_layout(void)
{
    m1_cmd_backward = NULL;
    m1_cmd_forward = NULL;
    m1_cmd_speed = NULL;
    m1_cmd_stop = NULL;
    m1_lbl_parameter = NULL;
    m1_lbl_speed = NULL;
    m1_lbl_status = NULL;
    masterview = NULL;
    view_left = NULL;
    view_middle = NULL;
    view_right = NULL;

    lv_obj_t *screen = lv_obj_create(NULL);
    lv_obj_set_size(screen, 1280, 800);
    lv_obj_t *masterview_obj_obj = lv_obj_create(screen);
    masterview = masterview_obj_obj;
    static int32_t masterview_obj_obj_col_dsc[] = { 300, LV_GRID_FR(1), 300, LV_GRID_TEMPLATE_LAST };
    static int32_t masterview_obj_obj_row_dsc[] = { LV_GRID_FR(1), LV_GRID_TEMPLATE_LAST };
    lv_obj_set_grid_dsc_array(masterview_obj_obj, masterview_obj_obj_col_dsc, masterview_obj_obj_row_dsc);
    lv_obj_set_size(masterview_obj_obj, lv_pct(100), lv_pct(100));
    lv_obj_set_layout(masterview_obj_obj, LV_LAYOUT_GRID);
    lv_obj_set_style_pad_all(masterview_obj_obj, 10, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_pad_column(masterview_obj_obj, 10, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *view_left_obj_obj = lv_obj_create(masterview_obj_obj);
    view_left = view_left_obj_obj;
    lv_obj_set_style_border_width(view_left_obj_obj, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_left_obj_obj, LV_GRID_ALIGN_STRETCH, 0, 1, LV_GRID_ALIGN_STRETCH, 0, 1);
    lv_obj_set_style_pad_all(view_left_obj_obj, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_left_obj_obj, 0, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_1 = lv_label_create(view_left_obj_obj);
    lv_label_set_text(label_1, "Links");
    lv_obj_set_x(label_1, 0);
    lv_obj_set_y(label_1, 0);
    lv_obj_t *m1_cmd_backward_obj_obj = lv_button_create(view_left_obj_obj);
    m1_cmd_backward = m1_cmd_backward_obj_obj;
    lv_obj_set_size(m1_cmd_backward_obj_obj, 100, LV_SIZE_CONTENT);
    lv_obj_set_y(m1_cmd_backward_obj_obj, 40);
    lv_obj_t *label_2 = lv_label_create(m1_cmd_backward_obj_obj);
    lv_label_set_text(label_2, "M1 LINKS");
    lv_obj_t *m1_cmd_stop_obj_obj = lv_button_create(view_left_obj_obj);
    m1_cmd_stop = m1_cmd_stop_obj_obj;
    lv_obj_set_size(m1_cmd_stop_obj_obj, 100, LV_SIZE_CONTENT);
    lv_obj_set_y(m1_cmd_stop_obj_obj, 100);
    lv_obj_t *label_3 = lv_label_create(m1_cmd_stop_obj_obj);
    lv_label_set_text(label_3, "M1 STOP");
    lv_obj_t *m1_cmd_forward_obj_obj = lv_button_create(view_left_obj_obj);
    m1_cmd_forward = m1_cmd_forward_obj_obj;
    lv_obj_set_size(m1_cmd_forward_obj_obj, 100, LV_SIZE_CONTENT);
    lv_obj_set_y(m1_cmd_forward_obj_obj, 160);
    lv_obj_t *label_4 = lv_label_create(m1_cmd_forward_obj_obj);
    lv_label_set_text(label_4, "M1 RECHTS");
    lv_obj_t *m1_cmd_speed_obj_obj = lv_slider_create(view_left_obj_obj);
    m1_cmd_speed = m1_cmd_speed_obj_obj;
    lv_obj_set_size(m1_cmd_speed_obj_obj, 220, 30);
    lv_slider_set_range(m1_cmd_speed_obj_obj, 0, 100);
    lv_obj_set_x(m1_cmd_speed_obj_obj, 20);
    lv_obj_set_y(m1_cmd_speed_obj_obj, 220);
    lv_obj_t *view_middle_obj_obj = lv_obj_create(masterview_obj_obj);
    view_middle = view_middle_obj_obj;
    lv_obj_set_style_border_width(view_middle_obj_obj, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_middle_obj_obj, LV_GRID_ALIGN_STRETCH, 1, 1, LV_GRID_ALIGN_STRETCH, 0, 1);
    lv_obj_set_style_pad_all(view_middle_obj_obj, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_middle_obj_obj, 0, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_5 = lv_label_create(view_middle_obj_obj);
    lv_label_set_text(label_5, "Mitte");
    lv_obj_set_x(label_5, 0);
    lv_obj_set_y(label_5, 0);
    lv_obj_t *label_6 = lv_label_create(view_middle_obj_obj);
    lv_label_set_text(label_6, "Motorstatus:");
    lv_obj_set_y(label_6, 50);
    lv_obj_t *m1_lbl_status_obj_obj = lv_label_create(view_middle_obj_obj);
    m1_lbl_status = m1_lbl_status_obj_obj;
    lv_label_set_text(m1_lbl_status_obj_obj, "Unknown");
    lv_obj_set_x(m1_lbl_status_obj_obj, 140);
    lv_obj_set_y(m1_lbl_status_obj_obj, 50);
    lv_obj_t *label_7 = lv_label_create(view_middle_obj_obj);
    lv_label_set_text(label_7, "Aktions Param:");
    lv_obj_set_y(label_7, 80);
    lv_obj_t *m1_lbl_parameter_obj_obj = lv_label_create(view_middle_obj_obj);
    m1_lbl_parameter = m1_lbl_parameter_obj_obj;
    lv_label_set_text(m1_lbl_parameter_obj_obj, "Unknown");
    lv_obj_set_x(m1_lbl_parameter_obj_obj, 140);
    lv_obj_set_y(m1_lbl_parameter_obj_obj, 80);
    lv_obj_t *label_8 = lv_label_create(view_middle_obj_obj);
    lv_label_set_text(label_8, "Geschwindigkeit:");
    lv_obj_set_y(label_8, 110);
    lv_obj_t *m1_lbl_speed_obj_obj = lv_label_create(view_middle_obj_obj);
    m1_lbl_speed = m1_lbl_speed_obj_obj;
    lv_label_set_text(m1_lbl_speed_obj_obj, "Unknown");
    lv_obj_set_x(m1_lbl_speed_obj_obj, 140);
    lv_obj_set_y(m1_lbl_speed_obj_obj, 110);
    lv_obj_t *view_right_obj_obj = lv_obj_create(masterview_obj_obj);
    view_right = view_right_obj_obj;
    lv_obj_set_style_border_width(view_right_obj_obj, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_right_obj_obj, LV_GRID_ALIGN_STRETCH, 2, 1, LV_GRID_ALIGN_STRETCH, 0, 1);
    lv_obj_set_style_pad_all(view_right_obj_obj, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_right_obj_obj, 0, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_9 = lv_label_create(view_right_obj_obj);
    lv_label_set_text(label_9, "Rechts");
    lv_obj_set_x(label_9, 0);
    lv_obj_set_y(label_9, 0);

    static ui_start_event_binding_t m1_cmd_backward_clicked_binding =
    {
        .source = M1_CMD_BACKWARD,
        .action = UI_START_ACTION_BACKWARD,
        .parameter_type = UI_START_PARAM_TYPE_INT32,
        .parameter = { .int32_value = 100 },
        .value_type = UI_START_PARAM_TYPE_NONE,
        .value_source = UI_START_VALUE_SOURCE_NONE,
        .value = { .int32_value = 0 }
    };
    lv_obj_add_event_cb(m1_cmd_backward, ui_start_dispatcher, LV_EVENT_CLICKED, &m1_cmd_backward_clicked_binding);
    static ui_start_event_binding_t m1_cmd_stop_clicked_binding =
    {
        .source = M1_CMD_STOP,
        .action = UI_START_ACTION_STOP,
        .parameter_type = UI_START_PARAM_TYPE_NONE,
        .parameter = { .int32_value = 0 },
        .value_type = UI_START_PARAM_TYPE_NONE,
        .value_source = UI_START_VALUE_SOURCE_NONE,
        .value = { .int32_value = 0 }
    };
    lv_obj_add_event_cb(m1_cmd_stop, ui_start_dispatcher, LV_EVENT_CLICKED, &m1_cmd_stop_clicked_binding);
    static ui_start_event_binding_t m1_cmd_forward_clicked_binding =
    {
        .source = M1_CMD_FORWARD,
        .action = UI_START_ACTION_FORWARD,
        .parameter_type = UI_START_PARAM_TYPE_INT32,
        .parameter = { .int32_value = 200 },
        .value_type = UI_START_PARAM_TYPE_NONE,
        .value_source = UI_START_VALUE_SOURCE_NONE,
        .value = { .int32_value = 0 }
    };
    lv_obj_add_event_cb(m1_cmd_forward, ui_start_dispatcher, LV_EVENT_CLICKED, &m1_cmd_forward_clicked_binding);
    static ui_start_event_binding_t m1_cmd_speed_released_binding =
    {
        .source = M1_CMD_SPEED,
        .action = UI_START_ACTION_SPEED,
        .parameter_type = UI_START_PARAM_TYPE_TEXT,
        .parameter = { .text_value = "WARNING" },
        .value_type = UI_START_PARAM_TYPE_INT32,
        .value_source = UI_START_VALUE_SOURCE_SLIDER_VALUE,
        .value = { .int32_value = 0 }
    };
    lv_obj_add_event_cb(m1_cmd_speed, ui_start_dispatcher, LV_EVENT_RELEASED, &m1_cmd_speed_released_binding);

    lv_screen_load(screen);
}

void ui_start_init(void)
{
    create_layout();
}
