#include "controller.h"
#include "machine.h"
#include "message.h"
#include "esp_log.h"
#include <stdio.h>
#include <string.h>

static const char *TAG = "controller";
static char last_speed_parameter[32] = "-";

/* --------------------------------------------------------------------------
   MCU-Projekt — feste Datei, nicht generiert

   Fluss:
     Display → control_queue → controller_handle_message()
                             → prüft Logik / Regeln
                             → sendet app_message_t → fieldbus_queue

     Fieldbus → control_queue → controller_handle_fieldbus_status()
                              → entscheidet ob Display-Update nötig
                              → sendet display_message_t → display_queue
   -------------------------------------------------------------------------- */

void control_init(void)
{
    ESP_LOGI(TAG, "Controller bereit");
}

/*
 * Eingehende Message vom Display-Event.
 * CONTRACT: msg->source = UI_START_OBJ_* identifiziert das Widget.
 * Hier prüft der MCU-Entwickler die Regeln und entscheidet
 * ob die Aktion an den Fieldbus weitergegeben wird.
 */
void control_handle_message(const app_message_t *msg)
{
    if (msg == NULL) return;

    ESP_LOGI(TAG, "Display-Event: source=%d action=%d",
             msg->source, msg->action);

    if (msg->action == UI_START_ACTION_SPEED)
    {
        int32_t speed_value = 0;
        bool has_speed_value = false;

        /*
         * Der RTOS-Generator trennt zwei Quellen:
         * - msg->value         = Laufzeitwert des Widgets, z. B. Sliderwert
         * - msg->parameter     = optionaler Zusatzwert aus dem Event-Editor
         *
         * Damit kann ein Slider gleichzeitig "Geschwindigkeit 73" und den
         * Zusatzparameter "WARNING" transportieren.
         */
        if (msg->value_type == UI_START_PARAM_TYPE_INT32)
        {
            speed_value = msg->value.int32_value;
            has_speed_value = true;
        }
        else if (msg->parameter_type == UI_START_PARAM_TYPE_INT32)
        {
            speed_value = msg->parameter.int32_value;
            has_speed_value = true;
        }

        if (msg->parameter_type == UI_START_PARAM_TYPE_TEXT &&
            msg->parameter.text_value != NULL)
        {
            ESP_LOGI(TAG, "Speed Zusatzparameter: %s", msg->parameter.text_value);
            snprintf(last_speed_parameter, sizeof(last_speed_parameter), "%s", msg->parameter.text_value);

            display_message_t parameter_display_msg = {
                .target       = M1_LBL_PARAMETER,
                .type         = DISPLAY_UPDATE_TEXT,
                .payload.text = last_speed_parameter,
            };

            if (xQueueSend(display_queue, &parameter_display_msg, 0) != pdTRUE)
                ESP_LOGW(TAG, "Display-Queue voll");
        }

        if (has_speed_value)
        {
            static char speed_text[16];
            snprintf(speed_text, sizeof(speed_text), "%ld", (long)speed_value);

            display_message_t display_msg = {
                .target       = M1_LBL_SPEED,
                .type         = DISPLAY_UPDATE_TEXT,
                .payload.text = speed_text,
            };

            if (xQueueSend(display_queue, &display_msg, 0) != pdTRUE)
                ESP_LOGW(TAG, "Display-Queue voll");
        }
    }

    /* Regelprüfung hier einfügen wenn nötig */

    /* Weiterleiten an Fieldbus */
    if (xQueueSend(fieldbus_queue, msg, 0) != pdTRUE)
    {
        ESP_LOGW(TAG, "Fieldbus-Queue voll");
    }
}

/*
 * Status-Rückmeldung vom Fieldbus nach Hardware-Ausführung.
 * CONTRACT: msg->source = UI_START_OBJ_* — gleicher Enum wie Display-Event.
 * Controller entscheidet ob und was ans Display gemeldet wird.
 */
void control_handle_fieldbus_status(const fieldbus_status_t *msg)
{
    if (msg == NULL) return;

    ESP_LOGI(TAG, "Fieldbus-Status: source=%d state=%d",
             msg->source, msg->state);

    /* Display-Update aus Fieldbus-Zustand ableiten */
    ui_machine_binding_t *binding = &machine_bindings[msg->source];
    if (binding->motor == NULL) return;

    const char *status_text = NULL;
    const char *parameter_text = "-";
    switch (msg->state)
    {
        case MOTOR_FORWARD:  status_text = "Vorwärts";  break;
        case MOTOR_BACKWARD: status_text = "Rückwärts"; break;
        case MOTOR_STOP:
        default:             status_text = "Stop";       break;
    }

    switch (msg->source)
    {
        case M1_CMD_BACKWARD: parameter_text = "100"; break;
        case M1_CMD_FORWARD:  parameter_text = "200"; break;
        case M1_CMD_STOP:     parameter_text = "-"; break;
        /* Beim Speed-Beispiel soll der zuletzt empfangene Zusatzparameter
           sichtbar bleiben und nicht vom spaeteren Status-Refresh verloren
           gehen. */
        case M1_CMD_SPEED:    parameter_text = last_speed_parameter; break;
        default:              parameter_text = "-"; break;
    }

    display_message_t display_msg = {
        .target       = binding->status_label,
        .type         = DISPLAY_UPDATE_TEXT,
        .payload.text = status_text,
    };

    if (xQueueSend(display_queue, &display_msg, 0) != pdTRUE)
        ESP_LOGW(TAG, "Display-Queue voll");

    display_message_t parameter_msg = {
        .target       = M1_LBL_PARAMETER,
        .type         = DISPLAY_UPDATE_TEXT,
        .payload.text = parameter_text,
    };

    if (xQueueSend(display_queue, &parameter_msg, 0) != pdTRUE)
        ESP_LOGW(TAG, "Display-Queue voll");
}
