#include "lvgl.h"

lv_theme_t * theme_project_create(lv_display_t * disp)
{
    return lv_theme_default_init(
        disp,
        lv_color_hex(0xd72638),
        lv_color_hex(0x2fbf71),
        false,
        LV_FONT_DEFAULT);
}
