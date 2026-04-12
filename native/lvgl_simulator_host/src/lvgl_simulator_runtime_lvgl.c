#include "lvgl_simulator_build_config.h"
#include "lvgl_simulator_runtime.h"
#include "lvgl_simulator_runtime_lvgl_internal.h"

#include <stdio.h>
#include <string.h>

bool lvgl_simulator_runtime_init(lvgl_simulator_runtime_t *runtime)
{
    if (runtime == NULL) {
        return false;
    }

    memset(runtime, 0, sizeof(*runtime));
    runtime->initialized = true;

    printf("LVGL runtime selected.\n");
    printf(
        "Build configuration: SDL2=%d LVGL=%d\n",
        LVGL_SIMULATOR_HAS_SDL2,
        LVGL_SIMULATOR_HAS_LVGL);

    if (!lvgl_runtime_prepare_platform(runtime)) {
        return false;
    }

    if (!lvgl_runtime_initialize_lvgl(runtime)) {
        return false;
    }

    if (!lvgl_runtime_present_boot_screen(runtime)) {
        return false;
    }

    fflush(stdout);
    return true;
}

int lvgl_simulator_runtime_run_main_loop(lvgl_simulator_runtime_t *runtime)
{
    if(runtime == NULL || !runtime->initialized) {
        return 1;
    }

    return lvgl_runtime_run_platform_loop(runtime);
}

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
    size_t status_message_size)
{
    if (runtime == NULL || !runtime->initialized || document_name == NULL || content == NULL) {
        if (status_message != NULL && status_message_size > 0) {
            snprintf(status_message, status_message_size, "LVGL runtime is not initialized.");
        }

        return false;
    }

    snprintf(runtime->last_document_name, sizeof(runtime->last_document_name), "%s", document_name);
    runtime->last_content_length = strlen(content);
    runtime->requested_screen_width = screen_width;
    runtime->requested_screen_height = screen_height;
    runtime->requested_zoom_percent = zoom_percent;

    printf(
        "LVGL runtime received %s for '%s' with %zu content chars. target_size=%dx%d zoom=%d%%\n",
        force_full_reload ? "reload" : "render",
        runtime->last_document_name,
        runtime->last_content_length,
        runtime->requested_screen_width,
        runtime->requested_screen_height,
        runtime->requested_zoom_percent);

    if(runtime->requested_screen_width > 0 && runtime->requested_screen_height > 0) {
        lvgl_runtime_request_display_size(
            runtime->requested_screen_width,
            runtime->requested_screen_height);
    }

    if(runtime->requested_zoom_percent > 0) {
        lvgl_runtime_request_zoom_percent(runtime->requested_zoom_percent);
    }

    if(reset_window_to_target_size) {
        lvgl_runtime_request_reset_window_to_target_size();
    }

    const bool success = lvgl_runtime_load_screen_from_generated_c(
        runtime,
        runtime->last_document_name,
        content,
        force_full_reload,
        status_message,
        status_message_size);

    fflush(stdout);
    return success;
}

void lvgl_simulator_runtime_request_shutdown(lvgl_simulator_runtime_t *runtime)
{
    (void)runtime;
    lvgl_runtime_request_platform_shutdown();
}

void lvgl_simulator_runtime_highlight(lvgl_simulator_runtime_t *runtime, const char *object_id)
{
    if(runtime == NULL || !runtime->initialized) {
        return;
    }

    if(lvgl_runtime_lock()) {
        lvgl_runtime_apply_highlight(object_id);
        lvgl_runtime_unlock();
    }
}

void lvgl_simulator_runtime_shutdown(lvgl_simulator_runtime_t *runtime)
{
    if (runtime == NULL || !runtime->initialized) {
        return;
    }

    printf(
        "LVGL runtime shutdown. last_document='%s' last_content_length=%zu\n",
        runtime->last_document_name[0] != '\0' ? runtime->last_document_name : "none",
        runtime->last_content_length);
    lvgl_runtime_shutdown_platform(runtime);
    fflush(stdout);

    runtime->initialized = false;
}
