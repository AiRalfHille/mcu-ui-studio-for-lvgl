#pragma once

#include <stdbool.h>
#include <stdint.h>

/* --------------------------------------------------------------------------
   GPIO-Pins
   -------------------------------------------------------------------------- */

#define PIN_M1_FORWARD   4
#define PIN_M1_BACKWARD  5

/* --------------------------------------------------------------------------
   Motor-Zustand
   -------------------------------------------------------------------------- */

typedef enum
{
    MOTOR_STOP = 0,
    MOTOR_FORWARD,
    MOTOR_BACKWARD
} motor_state_t;

/* --------------------------------------------------------------------------
   Motor-Struktur
   -------------------------------------------------------------------------- */

typedef struct
{
    struct
    {
        uint8_t pin_forward;
        uint8_t pin_backward;
    } config;

    motor_state_t state;

} motor_t;

/* --------------------------------------------------------------------------
   Maschinen-Struktur
   -------------------------------------------------------------------------- */

typedef struct
{
    motor_t m1;

} machine_t;

/* --------------------------------------------------------------------------
   Globale Maschineninstanz
   -------------------------------------------------------------------------- */

extern machine_t machine;

/* --------------------------------------------------------------------------
   Binding — UI-Objekt → Hardware

   CONTRACT: Verbindung zwischen Display-Contract und Machine.
   Der MCU-Entwickler trägt hier ein welcher UI_START_OBJ_* Enum-Wert
   auf welches Hardware-Objekt in der Machine zeigt.
   Einmalig befüllen — danach nur bei neuen Widgets ergänzen.
   -------------------------------------------------------------------------- */

#include "ui_start_contract.h"

typedef struct
{
    motor_t           *motor;
    ui_start_object_t  status_label;
} ui_machine_binding_t;

extern ui_machine_binding_t machine_bindings[UI_START_OBJ_COUNT];

/* --------------------------------------------------------------------------
   Öffentliche Funktionen
   -------------------------------------------------------------------------- */

void machine_init(void);
