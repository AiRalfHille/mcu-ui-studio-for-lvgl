#pragma once

#include "message.h"

void control_init(void);
void control_handle_message(const app_message_t *msg);
void control_handle_fieldbus_status(const fieldbus_status_t *msg);
