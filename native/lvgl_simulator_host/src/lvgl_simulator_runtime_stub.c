#include "lvgl_simulator_runtime.h"

#include <stdio.h>
#include <string.h>

#ifdef _WIN32
#include <windows.h>
#else
#include <unistd.h>
#endif

bool lvgl_simulator_runtime_init(lvgl_simulator_runtime_t *runtime)
{
    if (runtime == NULL) {
        return false;
    }

    memset(runtime, 0, sizeof(*runtime));
    runtime->initialized = true;

    printf("Simulator runtime initialized in stub mode.\n");
    fflush(stdout);
    return true;
}

int lvgl_simulator_runtime_run_main_loop(lvgl_simulator_runtime_t *runtime)
{
    (void)runtime;

    while(true) {
#ifdef _WIN32
        Sleep(1000);
#else
        sleep(1);
#endif
    }

    return 0;
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
            snprintf(status_message, status_message_size, "Simulator runtime is not initialized.");
        }

        return false;
    }

    snprintf(runtime->last_document_name, sizeof(runtime->last_document_name), "%s", document_name);
    runtime->last_content_length = strlen(content);
    runtime->requested_screen_width = screen_width;
    runtime->requested_screen_height = screen_height;
    runtime->requested_zoom_percent = zoom_percent;

    printf(
        "%s requested for document '%s' with %zu content chars. target_size=%dx%d zoom=%d%% reset=%s\n",
        force_full_reload ? "Reload" : "Render",
        runtime->last_document_name,
        runtime->last_content_length,
        runtime->requested_screen_width,
        runtime->requested_screen_height,
        runtime->requested_zoom_percent,
        reset_window_to_target_size ? "true" : "false");
    printf("Stub runtime accepted generated content and would now rebuild the LVGL screen tree.\n");
    fflush(stdout);

    if (status_message != NULL && status_message_size > 0) {
        snprintf(
            status_message,
            status_message_size,
            "Stub runtime accepted '%s' with %zu content chars for %dx%d at %d%% zoom.",
            runtime->last_document_name,
            runtime->last_content_length,
            runtime->requested_screen_width,
            runtime->requested_screen_height,
            runtime->requested_zoom_percent);
    }

    return true;
}

void lvgl_simulator_runtime_highlight(lvgl_simulator_runtime_t *runtime, const char *object_id)
{
    (void)runtime;
    (void)object_id;
}

void lvgl_simulator_runtime_request_shutdown(lvgl_simulator_runtime_t *runtime)
{
    (void)runtime;
}

void lvgl_simulator_runtime_shutdown(lvgl_simulator_runtime_t *runtime)
{
    if (runtime == NULL || !runtime->initialized) {
        return;
    }

    printf(
        "Simulator runtime shutdown. last_document='%s' last_content_length=%zu\n",
        runtime->last_document_name[0] != '\0' ? runtime->last_document_name : "none",
        runtime->last_content_length);
    fflush(stdout);

    runtime->initialized = false;
}
