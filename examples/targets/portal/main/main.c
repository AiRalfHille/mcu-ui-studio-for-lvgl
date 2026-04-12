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
   Eigene Module (Flow-Reihenfolge)
   -------------------------------------------------------------------------- */
#include "controller.h"
#include "display.h"
#include "fieldbus.h"
#include "machine.h"
#include "message.h"

 /* --------------------------------------------------------------------------
   Task Definitionen (gleiche Reihenfolge wie app_main)
   -------------------------------------------------------------------------- */

/* --------------------------------------------------------------------------
   CONTROL
   -------------------------------------------------------------------------- */

void control_task(void *pvParameters)
{
    control_message_t msg;

    ESP_LOGI("CONTROL", "control_task gestartet");

    while (1)
    {
        if (xQueueReceive(control_queue, &msg, portMAX_DELAY) == pdTRUE)
        {
            switch (msg.source_type)
            {
                case CONTROL_MSG_FROM_DISPLAY:
                    control_handle_message(&msg.payload.display);
                    break;
                case CONTROL_MSG_FROM_FIELDBUS:
                    control_handle_fieldbus_status(&msg.payload.fieldbus);
                    break;
            }
        }
    }
}

/* --------------------------------------------------------------------------
   DISPLAY INPUT
   -------------------------------------------------------------------------- */

void display_input_task(void *pvParameters)
{
    ESP_LOGI("DISPLAY", "display_input_task gestartet");
    ESP_LOGI("DISPLAY", "display_input_task wartet auf Implementierung und wird vorerst suspendiert");

    /* Noch keine Eingabe-Pipeline vorhanden.
       Die Task darf daher nicht in einer Busy-Loop laufen, sonst greift der Task-Watchdog. */
    vTaskSuspend(NULL);

}

/* --------------------------------------------------------------------------
   DISPLAY OUTPUT
   -------------------------------------------------------------------------- */

void display_output_task(void *pvParameters)
{
    display_message_t msg;

    ESP_LOGI("DISPLAY", "display_output_task gestartet");

    while (1)
    {
        if (xQueueReceive(display_queue, &msg, portMAX_DELAY) == pdTRUE)
        {
            display_handle_message(&msg);
        }
    }
}

/* --------------------------------------------------------------------------
   FIELDBUS INPUT
   -------------------------------------------------------------------------- */

void fieldbus_input_task(void *pvParameters)
{
    ESP_LOGI("FIELDBUS", "fieldbus_input_task gestartet");

    while (1)
    {
        fieldbus_poll();
        vTaskDelay(pdMS_TO_TICKS(20));
    }
}

/* --------------------------------------------------------------------------
   FIELDBUS OUTPUT
   -------------------------------------------------------------------------- */

void fieldbus_output_task(void *pvParameters)
{
    app_message_t msg;

    ESP_LOGI("FIELDBUS", "fieldbus_output_task gestartet");

    while (1)
    {
        if (xQueueReceive(fieldbus_queue, &msg, portMAX_DELAY) == pdTRUE)
        {
            fieldbus_handle_message(&msg);
        }
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
    bsp_display_unlock();

    machine_init();

    /* Messaging */
    message_init();
    control_init();
    fieldbus_init();

    /* ----------------------------------------------------------------------
       Tasks starten (gleiche Reihenfolge wie oben definiert)
       ---------------------------------------------------------------------- */

    xTaskCreatePinnedToCore(control_task,        "control_task",        4096, NULL, 5, NULL, 0);
    xTaskCreatePinnedToCore(display_input_task,  "display_input",       4096, NULL, 5, NULL, 1);
    xTaskCreatePinnedToCore(display_output_task, "display_output",      4096, NULL, 5, NULL, 1);
    xTaskCreatePinnedToCore(fieldbus_input_task, "fieldbus_input",      4096, NULL, 5, NULL, 0);
    xTaskCreatePinnedToCore(fieldbus_output_task,"fieldbus_output",     4096, NULL, 5, NULL, 0);

    printf("System gestartet\n");
}
