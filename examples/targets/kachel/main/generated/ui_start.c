#include "ui_start.h"
#include "ui_start_event.h"
#include "lvgl.h"

/* weitere Includes nach Bedarf */

lv_obj_t * m1_cmd_backward = NULL;
lv_obj_t * m1_cmd_forward = NULL;
lv_obj_t * m1_cmd_speed = NULL;
lv_obj_t * m1_cmd_stop = NULL;
lv_obj_t * m1_lbl_parameter = NULL;
lv_obj_t * m1_lbl_speed = NULL;
lv_obj_t * m1_lbl_status = NULL;
lv_obj_t * view_left = NULL;
lv_obj_t * view_middle = NULL;

static void create_layout(void)
{
    m1_cmd_backward = NULL;
    m1_cmd_forward = NULL;
    m1_cmd_speed = NULL;
    m1_cmd_stop = NULL;
    m1_lbl_parameter = NULL;
    m1_lbl_speed = NULL;
    m1_lbl_status = NULL;
    view_left = NULL;
    view_middle = NULL;

    lv_obj_t *screen = lv_obj_create(NULL);
    lv_obj_set_size(screen, 1280, 800);
    lv_obj_t *view_1 = lv_obj_create(screen);
    static int32_t view_1_col_dsc[] = { LV_GRID_FR(1), LV_GRID_FR(1), LV_GRID_FR(1), LV_GRID_FR(1), LV_GRID_TEMPLATE_LAST };
    static int32_t view_1_row_dsc[] = { LV_GRID_FR(1), LV_GRID_FR(1), LV_GRID_FR(1), LV_GRID_TEMPLATE_LAST };
    lv_obj_set_grid_dsc_array(view_1, view_1_col_dsc, view_1_row_dsc);
    lv_obj_set_size(view_1, lv_pct(100), lv_pct(100));
    lv_obj_set_layout(view_1, LV_LAYOUT_GRID);
    lv_obj_set_style_pad_all(view_1, 10, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_pad_column(view_1, 10, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_pad_row(view_1, 10, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *view_left_obj = lv_obj_create(view_1);
    view_left = view_left_obj;
    lv_obj_set_style_border_width(view_left_obj, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_left_obj, LV_GRID_ALIGN_STRETCH, 0, 1, LV_GRID_ALIGN_STRETCH, 0, 1);
    lv_obj_set_style_pad_all(view_left_obj, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_left_obj, 0, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *m1_cmd_backward_obj = lv_button_create(view_left_obj);
    m1_cmd_backward = m1_cmd_backward_obj;
    lv_obj_set_size(m1_cmd_backward_obj, 100, LV_SIZE_CONTENT);
    lv_obj_t *label_2 = lv_label_create(m1_cmd_backward_obj);
    lv_label_set_text(label_2, "M1 LINKS");
    lv_obj_t *m1_cmd_stop_obj = lv_button_create(view_left_obj);
    m1_cmd_stop = m1_cmd_stop_obj;
    lv_obj_set_size(m1_cmd_stop_obj, 100, LV_SIZE_CONTENT);
    lv_obj_set_y(m1_cmd_stop_obj, 60);
    lv_obj_t *label_3 = lv_label_create(m1_cmd_stop_obj);
    lv_label_set_text(label_3, "M1 STOP");
    lv_obj_t *m1_cmd_forward_obj = lv_button_create(view_left_obj);
    m1_cmd_forward = m1_cmd_forward_obj;
    lv_obj_set_size(m1_cmd_forward_obj, 100, LV_SIZE_CONTENT);
    lv_obj_set_y(m1_cmd_forward_obj, 120);
    lv_obj_t *label_4 = lv_label_create(m1_cmd_forward_obj);
    lv_label_set_text(label_4, "M1 RECHTS");
    lv_obj_t *m1_cmd_speed_obj = lv_slider_create(view_left_obj);
    m1_cmd_speed = m1_cmd_speed_obj;
    lv_obj_set_size(m1_cmd_speed_obj, 220, 30);
    lv_slider_set_range(m1_cmd_speed_obj, 0, 100);
    lv_obj_set_x(m1_cmd_speed_obj, 20);
    lv_obj_set_y(m1_cmd_speed_obj, 180);
    lv_obj_t *view_middle_obj = lv_obj_create(view_1);
    view_middle = view_middle_obj;
    lv_obj_set_style_border_width(view_middle_obj, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_middle_obj, LV_GRID_ALIGN_STRETCH, 1, 1, LV_GRID_ALIGN_STRETCH, 0, 1);
    lv_obj_set_style_pad_all(view_middle_obj, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_middle_obj, 0, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_5 = lv_label_create(view_middle_obj);
    lv_label_set_text(label_5, "Mitte");
    lv_obj_set_x(label_5, 0);
    lv_obj_set_y(label_5, 0);
    lv_obj_t *label_6 = lv_label_create(view_middle_obj);
    lv_label_set_text(label_6, "Motorstatus:");
    lv_obj_set_y(label_6, 50);
    lv_obj_t *m1_lbl_status_obj = lv_label_create(view_middle_obj);
    m1_lbl_status = m1_lbl_status_obj;
    lv_label_set_text(m1_lbl_status_obj, "Unknown");
    lv_obj_set_x(m1_lbl_status_obj, 140);
    lv_obj_set_y(m1_lbl_status_obj, 50);
    lv_obj_t *label_7 = lv_label_create(view_middle_obj);
    lv_label_set_text(label_7, "Aktions Param:");
    lv_obj_set_y(label_7, 80);
    lv_obj_t *m1_lbl_parameter_obj = lv_label_create(view_middle_obj);
    m1_lbl_parameter = m1_lbl_parameter_obj;
    lv_label_set_text(m1_lbl_parameter_obj, "Unknown");
    lv_obj_set_x(m1_lbl_parameter_obj, 140);
    lv_obj_set_y(m1_lbl_parameter_obj, 80);
    lv_obj_t *label_8 = lv_label_create(view_middle_obj);
    lv_label_set_text(label_8, "Geschwindigkeit:");
    lv_obj_set_y(label_8, 110);
    lv_obj_t *m1_lbl_speed_obj = lv_label_create(view_middle_obj);
    m1_lbl_speed = m1_lbl_speed_obj;
    lv_label_set_text(m1_lbl_speed_obj, "Unknown");
    lv_obj_set_x(m1_lbl_speed_obj, 140);
    lv_obj_set_y(m1_lbl_speed_obj, 110);
    lv_obj_t *view_9 = lv_obj_create(view_1);
    lv_obj_set_style_border_width(view_9, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_9, LV_GRID_ALIGN_STRETCH, 2, 1, LV_GRID_ALIGN_STRETCH, 0, 1);
    lv_obj_set_style_pad_all(view_9, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_9, 0, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_10 = lv_label_create(view_9);
    lv_label_set_text(label_10, "Kachel 3");
    lv_obj_set_x(label_10, 0);
    lv_obj_set_y(label_10, 0);
    lv_obj_t *view_11 = lv_obj_create(view_1);
    lv_obj_set_style_border_width(view_11, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_11, LV_GRID_ALIGN_STRETCH, 3, 1, LV_GRID_ALIGN_STRETCH, 0, 1);
    lv_obj_set_style_pad_all(view_11, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_11, 0, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_12 = lv_label_create(view_11);
    lv_label_set_text(label_12, "Kachel 4");
    lv_obj_set_x(label_12, 0);
    lv_obj_set_y(label_12, 0);
    lv_obj_t *view_13 = lv_obj_create(view_1);
    lv_obj_set_style_border_width(view_13, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_13, LV_GRID_ALIGN_STRETCH, 0, 1, LV_GRID_ALIGN_STRETCH, 1, 1);
    lv_obj_set_style_pad_all(view_13, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_13, 0, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_14 = lv_label_create(view_13);
    lv_label_set_text(label_14, "Kachel 5");
    lv_obj_set_x(label_14, 0);
    lv_obj_set_y(label_14, 0);
    lv_obj_t *view_15 = lv_obj_create(view_1);
    lv_obj_set_style_border_width(view_15, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_15, LV_GRID_ALIGN_STRETCH, 1, 1, LV_GRID_ALIGN_STRETCH, 1, 1);
    lv_obj_set_style_pad_all(view_15, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_15, 0, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_16 = lv_label_create(view_15);
    lv_label_set_text(label_16, "Kachel 6");
    lv_obj_set_x(label_16, 0);
    lv_obj_set_y(label_16, 0);
    lv_obj_t *view_17 = lv_obj_create(view_1);
    lv_obj_set_style_border_width(view_17, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_17, LV_GRID_ALIGN_STRETCH, 2, 1, LV_GRID_ALIGN_STRETCH, 1, 1);
    lv_obj_set_style_pad_all(view_17, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_17, 0, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_18 = lv_label_create(view_17);
    lv_label_set_text(label_18, "Kachel 7");
    lv_obj_set_x(label_18, 0);
    lv_obj_set_y(label_18, 0);
    lv_obj_t *view_19 = lv_obj_create(view_1);
    lv_obj_set_style_border_width(view_19, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_19, LV_GRID_ALIGN_STRETCH, 3, 1, LV_GRID_ALIGN_STRETCH, 1, 1);
    lv_obj_set_style_pad_all(view_19, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_19, 0, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_20 = lv_label_create(view_19);
    lv_label_set_text(label_20, "Kachel 8");
    lv_obj_set_x(label_20, 0);
    lv_obj_set_y(label_20, 0);
    lv_obj_t *view_21 = lv_obj_create(view_1);
    lv_obj_set_style_bg_color(view_21, lv_color_hex(0xffffff), LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_border_color(view_21, lv_color_hex(0xd9e4ec), LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_border_width(view_21, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_21, LV_GRID_ALIGN_STRETCH, 0, 1, LV_GRID_ALIGN_STRETCH, 2, 1);
    lv_obj_set_style_pad_all(view_21, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_21, 14, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_22 = lv_label_create(view_21);
    lv_label_set_text(label_22, "Kachel 9");
    lv_obj_set_x(label_22, 0);
    lv_obj_set_y(label_22, 0);
    lv_obj_t *view_23 = lv_obj_create(view_1);
    lv_obj_set_style_bg_color(view_23, lv_color_hex(0xffffff), LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_border_color(view_23, lv_color_hex(0xd9e4ec), LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_border_width(view_23, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_23, LV_GRID_ALIGN_STRETCH, 1, 1, LV_GRID_ALIGN_STRETCH, 2, 1);
    lv_obj_set_style_pad_all(view_23, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_23, 14, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_24 = lv_label_create(view_23);
    lv_label_set_text(label_24, "Kachel 10");
    lv_obj_set_x(label_24, 0);
    lv_obj_set_y(label_24, 0);
    lv_obj_t *view_25 = lv_obj_create(view_1);
    lv_obj_set_style_bg_color(view_25, lv_color_hex(0xffffff), LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_border_color(view_25, lv_color_hex(0xd9e4ec), LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_border_width(view_25, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_25, LV_GRID_ALIGN_STRETCH, 2, 1, LV_GRID_ALIGN_STRETCH, 2, 1);
    lv_obj_set_style_pad_all(view_25, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_25, 14, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_26 = lv_label_create(view_25);
    lv_label_set_text(label_26, "Kachel 11");
    lv_obj_set_x(label_26, 0);
    lv_obj_set_y(label_26, 0);
    lv_obj_t *view_27 = lv_obj_create(view_1);
    lv_obj_set_style_bg_color(view_27, lv_color_hex(0xffffff), LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_border_color(view_27, lv_color_hex(0xd9e4ec), LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_border_width(view_27, 1, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_grid_cell(view_27, LV_GRID_ALIGN_STRETCH, 3, 1, LV_GRID_ALIGN_STRETCH, 2, 1);
    lv_obj_set_style_pad_all(view_27, 18, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_set_style_radius(view_27, 14, LV_PART_MAIN | LV_STATE_DEFAULT);
    lv_obj_t *label_28 = lv_label_create(view_27);
    lv_label_set_text(label_28, "Kachel 12");
    lv_obj_set_x(label_28, 0);
    lv_obj_set_y(label_28, 0);

    static ui_start_event_binding_t m1_cmd_backward_clicked_binding =
    {
        .source_name = "m1_cmd_backward",
        .event_group = "m1",
        .event_type = "backward",
        .action = "ACTION_BACKWARD",
        .parameter_type = UI_START_PARAM_TYPE_INT32,
        .parameter = { .int32_value = 100 },
        .value_type = UI_START_PARAM_TYPE_NONE,
        .value_source = UI_START_VALUE_SOURCE_NONE,
        .value = { .int32_value = 0 }
    };
    lv_obj_add_event_cb(m1_cmd_backward, ui_start_m1_dispatcher, LV_EVENT_CLICKED, &m1_cmd_backward_clicked_binding);
    static ui_start_event_binding_t m1_cmd_forward_clicked_binding =
    {
        .source_name = "m1_cmd_forward",
        .event_group = "m1",
        .event_type = "forward",
        .action = "ACTION_FORWARD",
        .parameter_type = UI_START_PARAM_TYPE_INT32,
        .parameter = { .int32_value = 200 },
        .value_type = UI_START_PARAM_TYPE_NONE,
        .value_source = UI_START_VALUE_SOURCE_NONE,
        .value = { .int32_value = 0 }
    };
    lv_obj_add_event_cb(m1_cmd_forward, ui_start_m1_dispatcher, LV_EVENT_CLICKED, &m1_cmd_forward_clicked_binding);
    static ui_start_event_binding_t m1_cmd_speed_released_binding =
    {
        .source_name = "m1_cmd_speed",
        .event_group = "speed",
        .event_type = "",
        .action = "ACTION_SPEED",
        .parameter_type = UI_START_PARAM_TYPE_TEXT,
        .parameter = { .text_value = "WARNING" },
        .value_type = UI_START_PARAM_TYPE_INT32,
        .value_source = UI_START_VALUE_SOURCE_SLIDER_VALUE,
        .value = { .int32_value = 0 }
    };
    lv_obj_add_event_cb(m1_cmd_speed, ui_start_speed_dispatcher, LV_EVENT_RELEASED, &m1_cmd_speed_released_binding);
    static ui_start_event_binding_t m1_cmd_stop_clicked_binding =
    {
        .source_name = "m1_cmd_stop",
        .event_group = "m1",
        .event_type = "stop",
        .action = "ACTION_STOP",
        .parameter_type = UI_START_PARAM_TYPE_NONE,
        .parameter = { .int32_value = 0 },
        .value_type = UI_START_PARAM_TYPE_NONE,
        .value_source = UI_START_VALUE_SOURCE_NONE,
        .value = { .int32_value = 0 }
    };
    lv_obj_add_event_cb(m1_cmd_stop, ui_start_m1_dispatcher, LV_EVENT_CLICKED, &m1_cmd_stop_clicked_binding);

    lv_screen_load(screen);
}

void ui_start_init(void)
{
    create_layout();
}
