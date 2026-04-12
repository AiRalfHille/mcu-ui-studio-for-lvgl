#pragma once

/* --------------------------------------------------------------------------
   MCU-Projekt — feste Datei, nicht generiert
   -------------------------------------------------------------------------- */

#include "freertos/FreeRTOS.h"
#include "freertos/queue.h"
#include "ui_start_contract.h"
#include "machine.h"

/*
 * Display → Controller
 * Wrapper für control_queue — unterscheidet Absender
 */
typedef enum
{
    CONTROL_MSG_FROM_DISPLAY = 0,
    CONTROL_MSG_FROM_FIELDBUS
} control_msg_source_t;

/*
 * Fieldbus → Controller
 * Meldet den neuen Hardware-Zustand nach Ausführung eines Befehls
 * oder nach zyklischer Abfrage (fieldbus_poll).
 */
typedef struct
{
    ui_start_object_t  source;
    motor_state_t      state;
} fieldbus_status_t;

typedef struct
{
    control_msg_source_t source_type;
    union
    {
        app_message_t     display;
        fieldbus_status_t fieldbus;
    } payload;
} control_message_t;

/*
 * Controller → Display
 */
typedef enum
{
    DISPLAY_UPDATE_TEXT = 0,
    DISPLAY_UPDATE_VALUE,
    DISPLAY_UPDATE_BOOL,
    DISPLAY_UPDATE_VISIBLE,
    DISPLAY_UPDATE_ENABLED,
    DISPLAY_UPDATE_BRIGHTNESS,
} display_update_type_t;

typedef struct
{
    ui_start_object_t     target;
    display_update_type_t type;
    union
    {
        const char *text;
        int32_t     value;
        bool        flag;
        uint8_t     brightness;
    } payload;
} display_message_t;

extern QueueHandle_t control_queue;
extern QueueHandle_t display_queue;
extern QueueHandle_t fieldbus_queue;

void message_init(void);
