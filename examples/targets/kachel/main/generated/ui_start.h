#pragma once

#include "lvgl.h"

extern lv_obj_t * m1_cmd_backward;
extern lv_obj_t * m1_cmd_forward;
extern lv_obj_t * m1_cmd_speed;
extern lv_obj_t * m1_cmd_stop;
extern lv_obj_t * m1_lbl_parameter;
extern lv_obj_t * m1_lbl_speed;
extern lv_obj_t * m1_lbl_status;
extern lv_obj_t * view_left;
extern lv_obj_t * view_middle;

void ui_start_init(void);
