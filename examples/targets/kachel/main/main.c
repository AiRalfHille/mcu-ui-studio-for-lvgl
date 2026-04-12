#include <stdio.h>

/* --------------------------------------------------------------------------
   FreeRTOS
   -------------------------------------------------------------------------- */
#include "freertos/FreeRTOS.h"
#include "freertos/task.h"

/* --------------------------------------------------------------------------
   ESP / BSP
   -------------------------------------------------------------------------- */
#include "esp_log.h"
#include "bsp/esp-bsp.h"
#include "bsp/display.h"

/* --------------------------------------------------------------------------
   Eigene Module
   -------------------------------------------------------------------------- */
#include "display.h"
#include "app_logic.h"
#include "bsp/esp-bsp.h"

/* --------------------------------------------------------------------------
   Display Runtime Task
   -------------------------------------------------------------------------- */

#define DISPLAY_RUNTIME_TASK_STACK_SIZE 8192

void display_runtime_task(void *pvParameters)
{
    ESP_LOGI("DISPLAY", "display_runtime_task gestartet");

    while (1)
    {
        bsp_display_lock(-1);
        display_update();
        app_logic_tick();
        bsp_display_unlock();
        vTaskDelay(pdMS_TO_TICKS(5));
    }
}

/* --------------------------------------------------------------------------
   Einstiegspunkt
   -------------------------------------------------------------------------- */

void app_main(void)
{
    bsp_display_cfg_t cfg = {
        .lv_adapter_cfg = ESP_LV_ADAPTER_DEFAULT_CONFIG(),
        .rotation = ESP_LV_ADAPTER_ROTATE_90,
        .tear_avoid_mode = ESP_LV_ADAPTER_TEAR_AVOID_MODE_TRIPLE_PARTIAL,
        .touch_flags = {
            .swap_xy = 1,
            .mirror_x = 1,
            .mirror_y = 0
        }
    };

    /* Debug */
    esp_log_level_set("*", ESP_LOG_DEBUG);

    ESP_LOGI("MAIN", "System startet...");

    /* Display */
    bsp_display_start_with_config(&cfg);
    bsp_display_backlight_on();

    bsp_display_lock(-1);
    display_init();
    app_logic_init();
    bsp_display_unlock();

    xTaskCreatePinnedToCore(display_runtime_task, "display_runtime", DISPLAY_RUNTIME_TASK_STACK_SIZE, NULL, 5, NULL, 1);

    printf("System gestartet\n");
}
