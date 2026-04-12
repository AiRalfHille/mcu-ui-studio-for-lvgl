#include "display.h"

#include "bsp/esp-bsp.h"
#include "ui_start.h"

void display_init(void)
{
    ui_start_init();
}

void display_update(void)
{
    lv_timer_handler();
}
