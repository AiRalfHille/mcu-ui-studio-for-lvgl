#include "lvgl.h"

lv_theme_t * theme_project_create(lv_display_t * disp)
{
    return lv_theme_default_init(
        disp,
        lv_color_hex(0x2596be),
        lv_color_hex(0xff8a00),
        false,
        LV_FONT_DEFAULT);
}
