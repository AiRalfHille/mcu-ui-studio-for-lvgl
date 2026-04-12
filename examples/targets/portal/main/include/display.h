#pragma once

#include "message.h"

/*
 * Schnittstelle des Display-Moduls.
 *
 * Konkrete API folgt, sobald:
 * - LVGL-Setup
 * - generierte UI-Dateien
 * - Queue-Integration
 * genauer festgelegt sind.
 */

void display_init(void);
void display_handle_message(const display_message_t *msg);
