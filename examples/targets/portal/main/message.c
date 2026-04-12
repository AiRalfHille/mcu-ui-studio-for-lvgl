#include "message.h"

/*
 * Zentrale Queues der Beispiel-Runtime.
 *
 * control_queue:
 *   Display-Events und Fieldbus-Rueckmeldungen laufen hier zusammen und
 *   werden im Controller in fachliche Entscheidungen uebersetzt.
 *
 * display_queue:
 *   Controller sendet reine Anzeige-Updates an den Display-Task. Der
 *   Controller bleibt dadurch LVGL-frei.
 *
 * fieldbus_queue:
 *   Controller gibt gepruefte Runtime-Befehle an die Hardware-/Fieldbus-
 *   Ebene weiter.
 */
QueueHandle_t control_queue;
QueueHandle_t display_queue;
QueueHandle_t fieldbus_queue;

void message_init(void)
{
    /* Kleine Queue-Tiefen reichen fuer das Beispiel und halten den Ablauf gut
       sichtbar. In einer realen Anwendung werden sie projektbezogen skaliert. */
    control_queue  = xQueueCreate(10, sizeof(control_message_t));
    display_queue  = xQueueCreate(10, sizeof(display_message_t));
    fieldbus_queue = xQueueCreate(10, sizeof(app_message_t));
}
