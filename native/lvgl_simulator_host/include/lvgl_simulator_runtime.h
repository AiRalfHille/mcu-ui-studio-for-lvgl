#ifndef LVGL_SIMULATOR_RUNTIME_H
#define LVGL_SIMULATOR_RUNTIME_H

#include <stdbool.h>
#include <stddef.h>

typedef struct lvgl_simulator_runtime {
    char last_document_name[512];
    size_t last_content_length;
    int requested_screen_width;
    int requested_screen_height;
    int requested_zoom_percent;
    bool first_screen_presented;
    bool platform_ready;
    bool initialized;
} lvgl_simulator_runtime_t;

bool lvgl_simulator_runtime_init(lvgl_simulator_runtime_t *runtime);
int lvgl_simulator_runtime_run_main_loop(lvgl_simulator_runtime_t *runtime);
bool lvgl_simulator_runtime_render(
    lvgl_simulator_runtime_t *runtime,
    const char *document_name,
    const char *content,
    bool force_full_reload,
    int screen_width,
    int screen_height,
    int zoom_percent,
    bool reset_window_to_target_size,
    char *status_message,
    size_t status_message_size);
void lvgl_simulator_runtime_request_shutdown(lvgl_simulator_runtime_t *runtime);
void lvgl_simulator_runtime_shutdown(lvgl_simulator_runtime_t *runtime);
void lvgl_simulator_runtime_highlight(lvgl_simulator_runtime_t *runtime, const char *object_id);

#endif
