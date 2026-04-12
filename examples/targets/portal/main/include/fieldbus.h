#pragma once

#include "message.h"

void fieldbus_init(void);
void fieldbus_poll(void);
void fieldbus_handle_message(const app_message_t *msg);
