#include "driver/gpio.h"
#include "esp_log.h"

#include "machine.h"

#define TAG "machine"

/* --------------------------------------------------------------------------
   Globale Maschineninstanz
   -------------------------------------------------------------------------- */

machine_t machine;

/* --------------------------------------------------------------------------
   Interne Hilfsfunktion — Motor schalten
   -------------------------------------------------------------------------- */

static void motor_apply(motor_t *motor)
{
    switch (motor->state)
    {
        case MOTOR_FORWARD:
            gpio_set_level(motor->config.pin_forward,  1);
            gpio_set_level(motor->config.pin_backward, 0);
            break;

        case MOTOR_BACKWARD:
            gpio_set_level(motor->config.pin_forward,  0);
            gpio_set_level(motor->config.pin_backward, 1);
            break;

        case MOTOR_STOP:
        default:
            gpio_set_level(motor->config.pin_forward,  0);
            gpio_set_level(motor->config.pin_backward, 0);
            break;
    }
}

/* --------------------------------------------------------------------------
   Initialisierung
   -------------------------------------------------------------------------- */

void machine_init(void)
{
    ESP_LOGI(TAG, "Machine init...");

    machine.m1.config.pin_forward  = PIN_M1_FORWARD;
    machine.m1.config.pin_backward = PIN_M1_BACKWARD;
    machine.m1.state               = MOTOR_STOP;

    gpio_set_direction(PIN_M1_FORWARD,  GPIO_MODE_OUTPUT);
    gpio_set_direction(PIN_M1_BACKWARD, GPIO_MODE_OUTPUT);

    motor_apply(&machine.m1);

    ESP_LOGI(TAG, "Machine init fertig");
}

/* --------------------------------------------------------------------------
   Binding-Tabelle — UI-Objekt → Hardware

   CONTRACT: Hier verbindet der MCU-Entwickler den Screen-Contract
   mit der konkreten Hardware. Neue Widgets im Editor → neuer Enum-Eintrag
   → hier eintragen.
   -------------------------------------------------------------------------- */

ui_machine_binding_t machine_bindings[UI_START_OBJ_COUNT] = {
    [M1_CMD_BACKWARD] = { .motor = &machine.m1, .status_label = M1_LBL_STATUS },
    [M1_CMD_STOP]     = { .motor = &machine.m1, .status_label = M1_LBL_STATUS },
    [M1_CMD_FORWARD]  = { .motor = &machine.m1, .status_label = M1_LBL_STATUS },
    [M1_CMD_SPEED]    = { .motor = &machine.m1, .status_label = M1_LBL_STATUS },
    [M1_LBL_STATUS]   = { .motor = NULL,        .status_label = M1_LBL_STATUS },
    [M1_LBL_PARAMETER]= { .motor = NULL,        .status_label = M1_LBL_STATUS },
};
