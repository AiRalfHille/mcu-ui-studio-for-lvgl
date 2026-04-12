#include "fieldbus.h"
#include "machine.h"
#include "message.h"
#include "driver/gpio.h"
#include "esp_log.h"

static const char *TAG = "fieldbus";

/* --------------------------------------------------------------------------
   MCU-Projekt — feste Datei, nicht generiert

   Fluss:
     fieldbus_handle_message() — empfängt app_message_t von Controller
       → schaltet Hardware (GPIO)
       → aktualisiert machine struct
       → sendet Status zurück an control_queue

     fieldbus_poll() — zyklisch, liest Hardware-Eingänge
       → bei Änderung: sendet app_message_t → control_queue
   -------------------------------------------------------------------------- */

void fieldbus_init(void)
{
    /* ------------------------------------------------------------------
       Motor-Pins konfigurieren — PIN_M1_FORWARD / PIN_M1_BACKWARD in
       machine.h auf die echten GPIO-Nummern setzen, dann einkommentieren.
       ------------------------------------------------------------------ */

    // gpio_config_t io_conf = {
    //     .pin_bit_mask = (1ULL << PIN_M1_FORWARD) | (1ULL << PIN_M1_BACKWARD),
    //     .mode         = GPIO_MODE_OUTPUT,
    //     .pull_up_en   = GPIO_PULLUP_DISABLE,
    //     .pull_down_en = GPIO_PULLDOWN_DISABLE,
    //     .intr_type    = GPIO_INTR_DISABLE,
    // };
    // gpio_config(&io_conf);

    ESP_LOGI(TAG, "Fieldbus bereit");
}

/*
 * Ausführung eines Befehls vom Controller.
 * CONTRACT: msg->source = UI_START_OBJ_* → Binding-Tabelle → Hardware.
 * Nach Ausführung: Status zurück an control_queue.
 */
void fieldbus_handle_message(const app_message_t *msg)
{
    if (msg == NULL) return;

    ESP_LOGI(TAG, "Ausführen: source=%d action=%d", msg->source, msg->action);

    /* Binding → Hardware */
    ui_machine_binding_t *binding = &machine_bindings[msg->source];
    if (binding->motor == NULL) return;

    motor_t *motor = binding->motor;

    /* action → Motor-Zustand */
    switch (msg->action)
    {
        case UI_START_ACTION_BACKWARD:
            motor->state = MOTOR_BACKWARD;
            break;
        case UI_START_ACTION_SPEED:
            /*
             * Demonstrationspfad:
             * Der Contract und die Queue-Architektur koennen bereits einen
             * numerischen Stellwert transportieren. Im Beispiel wird daraus
             * aber noch kein echter PWM-/Antriebs-Sollwert geschrieben.
             */
            ESP_LOGI(TAG, "Speed event empfangen");
            break;
        case UI_START_ACTION_STOP:
            motor->state = MOTOR_STOP;
            break;
        case UI_START_ACTION_FORWARD:
            motor->state = MOTOR_FORWARD;
            break;
        default:
            ESP_LOGW(TAG, "Unbekannte action: %d", msg->action);
            return;
    }

    /* GPIO schalten */
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

    ESP_LOGI(TAG, "Motor geschaltet: state=%d", motor->state);

    /* Status zurück an Controller */
    control_message_t status = {
        .source_type             = CONTROL_MSG_FROM_FIELDBUS,
        .payload.fieldbus.source = msg->source,
        .payload.fieldbus.state  = motor->state,
    };

    if (xQueueSend(control_queue, &status, 0) != pdTRUE)
        ESP_LOGW(TAG, "Control-Queue voll");
}

/*
 * Zyklische Abfrage von Hardware-Eingängen (Sensoren, Endlagen etc.)
 * Platzhalter — bei Änderung app_message_t → control_queue senden.
 */
void fieldbus_poll(void)
{
    /* TODO: Hardware-Eingänge lesen und bei Änderung melden */
}
