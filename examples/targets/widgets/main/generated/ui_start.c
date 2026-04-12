#include "ui_start.h"
#include "lvgl.h"

/* weitere Includes nach Bedarf */

static void create_layout(void)
{
    lv_obj_t *screen = lv_obj_create(NULL);
    lv_obj_set_size(screen, 1280, 800);
    lv_obj_t *label_1 = lv_label_create(screen);
    lv_label_set_text(label_1, "Widget's");
    lv_obj_set_size(label_1, 100, LV_SIZE_CONTENT);
    lv_obj_set_x(label_1, 32);
    lv_obj_set_y(label_1, 24);
    lv_obj_t *button_2 = lv_button_create(screen);
    lv_obj_set_size(button_2, 100, 30);
    lv_obj_set_x(button_2, 30);
    lv_obj_set_y(button_2, 80);
    lv_obj_t *label_3 = lv_label_create(button_2);
    lv_obj_set_align(label_3, LV_ALIGN_CENTER);
    lv_label_set_text(label_3, "Button");
    lv_obj_t *slider_4 = lv_slider_create(screen);
    lv_obj_set_size(slider_4, 200, 30);
    lv_slider_set_range(slider_4, 0, 100);
    lv_slider_set_start_value(slider_4, 0, LV_ANIM_OFF);
    lv_slider_set_value(slider_4, 0, LV_ANIM_OFF);
    lv_obj_set_x(slider_4, 52);
    lv_obj_set_y(slider_4, 130);
    lv_obj_t *label_5 = lv_label_create(slider_4);
    lv_obj_set_align(label_5, LV_ALIGN_CENTER);
    lv_label_set_text(label_5, "Slider");
    lv_obj_t *bar_6 = lv_bar_create(screen);
    lv_obj_set_size(bar_6, 220, 30);
    lv_bar_set_range(bar_6, 0, 100);
    lv_bar_set_start_value(bar_6, 30, LV_ANIM_OFF);
    lv_obj_set_x(bar_6, 30);
    lv_obj_set_y(bar_6, 180);
    lv_obj_t *label_7 = lv_label_create(bar_6);
    lv_obj_set_align(label_7, LV_ALIGN_CENTER);
    lv_label_set_text(label_7, "30%");
    lv_obj_t *checkbox_8 = lv_checkbox_create(screen);
    lv_checkbox_set_text(checkbox_8, "mit Validierung");
    lv_obj_set_x(checkbox_8, 30);
    lv_obj_set_y(checkbox_8, 230);
    lv_obj_t *led_9 = lv_led_create(screen);
    lv_led_set_brightness(led_9, 70);
    lv_obj_add_flag(led_9, LV_OBJ_FLAG_CLICKABLE);
    lv_led_set_color(led_9, lv_color_hex(0xff0000));
    lv_obj_set_size(led_9, 30, 30);
    lv_obj_remove_flag(led_9, LV_OBJ_FLAG_HIDDEN);
    lv_obj_set_x(led_9, 30);
    lv_obj_set_y(led_9, 280);
    lv_obj_t *label_10 = lv_label_create(screen);
    lv_label_set_text(label_10, "LED");
    lv_obj_set_x(label_10, 70);
    lv_obj_set_y(label_10, 287);
    lv_obj_t *switch_11 = lv_switch_create(screen);
    lv_obj_set_size(switch_11, 70, 30);
    lv_obj_set_x(switch_11, 30);
    lv_obj_set_y(switch_11, 320);
    lv_obj_t *label_12 = lv_label_create(screen);
    lv_label_set_long_mode(label_12, LV_LABEL_LONG_MODE_SCROLL_CIRCULAR);
    lv_label_set_text(label_12, "Motorstatus:");
    lv_obj_set_size(label_12, 80, LV_SIZE_CONTENT);
    lv_obj_set_x(label_12, 320);
    lv_obj_set_y(label_12, 86);
    lv_obj_t *arc_13 = lv_arc_create(screen);
    lv_obj_set_size(arc_13, LV_SIZE_CONTENT, 100);
    lv_obj_set_x(arc_13, 320);
    lv_obj_set_y(arc_13, 130);
    lv_obj_t *label_14 = lv_label_create(arc_13);
    lv_label_set_text(label_14, "ARC");
    lv_obj_set_x(label_14, 32);
    lv_obj_set_y(label_14, 45);
    lv_obj_t *roller_15 = lv_roller_create(screen);
    lv_obj_set_x(roller_15, 320);
    lv_obj_set_y(roller_15, 230);
    lv_obj_t *label_16 = lv_label_create(roller_15);
    lv_label_set_text(label_16, "Roller");
    lv_obj_t *spinbox_17 = lv_spinbox_create(screen);
    lv_obj_set_x(spinbox_17, 500);
    lv_obj_set_y(spinbox_17, 80);
    lv_obj_t *spinner_18 = lv_spinner_create(screen);
    lv_obj_set_size(spinner_18, LV_SIZE_CONTENT, 90);
    lv_obj_set_x(spinner_18, 500);
    lv_obj_set_y(spinner_18, 130);
    lv_obj_t *label_19 = lv_label_create(spinner_18);
    lv_label_set_text(label_19, "Spinner");
    lv_obj_set_x(label_19, 16);
    lv_obj_set_y(label_19, 35);
    lv_obj_t *textArea_20 = lv_textarea_create(screen);
    lv_obj_set_size(textArea_20, 200, 120);
    lv_textarea_set_text(textArea_20, "Hello World ich bin eine Textarea");
    lv_obj_set_x(textArea_20, 500);
    lv_obj_set_y(textArea_20, 230);
    lv_obj_t *line_21 = lv_line_create(screen);
    lv_obj_remove_flag(line_21, LV_OBJ_FLAG_CLICKABLE);
    lv_obj_remove_flag(line_21, LV_OBJ_FLAG_HIDDEN);
    static lv_point_precise_t line_21_points[] = { { 0, 0 }, { 1190, 0 } };
    lv_line_set_points(line_21, line_21_points, 2);
    lv_obj_set_x(line_21, 30);
    lv_obj_set_y(line_21, 370);
    lv_line_set_y_invert(line_21, false);
    lv_obj_t *arcLabel_22 = lv_arclabel_create(screen);
    lv_obj_set_size(arcLabel_22, 100, 100);
    lv_arclabel_set_radius(arcLabel_22, 40);
    lv_arclabel_set_text(arcLabel_22, "Hello World");
    lv_obj_set_y(arcLabel_22, 500);
    lv_obj_t *scale_23 = lv_scale_create(screen);
    lv_obj_set_x(scale_23, 160);
    lv_obj_set_y(scale_23, 500);

    lv_screen_load(screen);
}

void ui_start_init(void)
{
    create_layout();
}
