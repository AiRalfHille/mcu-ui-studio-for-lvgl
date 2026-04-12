#ifndef LVGL_SIMULATOR_RUNTIME_LVGL_INTERNAL_H
#define LVGL_SIMULATOR_RUNTIME_LVGL_INTERNAL_H

#include "lvgl_simulator_runtime.h"

#include <stdbool.h>
#include <stddef.h>

bool lvgl_runtime_prepare_platform(lvgl_simulator_runtime_t *runtime);
bool lvgl_runtime_initialize_lvgl(lvgl_simulator_runtime_t *runtime);
bool lvgl_runtime_present_boot_screen(lvgl_simulator_runtime_t *runtime);
int lvgl_runtime_run_platform_loop(lvgl_simulator_runtime_t *runtime);
bool lvgl_runtime_load_screen_from_generated_c(
    lvgl_simulator_runtime_t *runtime,
    const char *document_name,
    const char *content,
    bool force_full_reload,
    char *status_message,
    size_t status_message_size);
bool lvgl_runtime_present_demo_screen(
    lvgl_simulator_runtime_t *runtime,
    const char *document_name,
    size_t content_length,
    char *status_message,
    size_t status_message_size);
void lvgl_runtime_apply_highlight(const char *object_id);
void lvgl_runtime_request_platform_shutdown(void);
void lvgl_runtime_request_display_size(int width, int height);
void lvgl_runtime_request_zoom_percent(int zoom_percent);
void lvgl_runtime_request_reset_window_to_target_size(void);
void lvgl_runtime_hide_window(void);
void lvgl_runtime_show_window(void);
bool lvgl_runtime_lock(void);
void lvgl_runtime_unlock(void);
void lvgl_runtime_shutdown_platform(lvgl_simulator_runtime_t *runtime);

#endif
