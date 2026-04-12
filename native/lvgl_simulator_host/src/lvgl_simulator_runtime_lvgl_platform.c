#include "lvgl_simulator_build_config.h"
#include "lvgl_simulator_runtime_lvgl_internal.h"

#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <unistd.h>

#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
#include <pthread.h>
#include <stdlib.h>
#include <math.h>

#include "lvgl.h"
#include "core/lv_group.h"
#include "lv_sdl_keyboard.h"
#include "lv_sdl_mouse.h"
#include "lv_sdl_mousewheel.h"
#include "lv_sdl_window.h"
#include <SDL2/SDL_video.h>
#endif

#define SIM_HOR_RES 1280
#define SIM_VER_RES 800

static volatile bool g_shutdown_requested;

#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
static pthread_mutex_t g_lvgl_mutex = PTHREAD_MUTEX_INITIALIZER;
static lv_display_t *g_display;
static lv_group_t *g_default_group;
static lv_indev_t *g_mouse_indev;
static lv_indev_t *g_mousewheel_indev;
static lv_indev_t *g_keyboard_indev;
static int g_pending_hor_res;
static int g_pending_ver_res;
static int g_pending_zoom_percent;
static int g_target_hor_res;
static int g_target_ver_res;
static bool g_pending_reset_window_to_target_size;
static bool g_pending_show_window;
static bool g_window_visible;
#endif

#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
static void simulator_lvgl_log_cb(lv_log_level_t level, const char *buf)
{
    const char *level_name = "log";
    switch(level) {
        case LV_LOG_LEVEL_TRACE: level_name = "trace"; break;
        case LV_LOG_LEVEL_INFO: level_name = "info"; break;
        case LV_LOG_LEVEL_WARN: level_name = "warn"; break;
        case LV_LOG_LEVEL_ERROR: level_name = "error"; break;
        case LV_LOG_LEVEL_USER: level_name = "user"; break;
        default: break;
    }

    printf("LVGL [%s] %s\n", level_name, buf);
    fflush(stdout);
}

static void apply_pending_display_size_locked(void)
{
    if(g_display == NULL || g_pending_hor_res <= 0 || g_pending_ver_res <= 0) {
        return;
    }

    if(lv_display_get_horizontal_resolution(g_display) == g_pending_hor_res &&
       lv_display_get_vertical_resolution(g_display) == g_pending_ver_res) {
        g_pending_hor_res = 0;
        g_pending_ver_res = 0;
        return;
    }

    printf(
        "Applying simulator window size %dx%d.\n",
        g_pending_hor_res,
        g_pending_ver_res);
    lv_display_set_resolution(g_display, g_pending_hor_res, g_pending_ver_res);
    g_pending_hor_res = 0;
    g_pending_ver_res = 0;
    fflush(stdout);
}

static void apply_pending_display_zoom_locked(void)
{
    if(g_display == NULL || g_pending_zoom_percent <= 0) {
        return;
    }

    printf("Applying simulator window zoom %d%%.\n", g_pending_zoom_percent);
    lv_sdl_window_set_zoom(g_display, (float)g_pending_zoom_percent / 100.0f);
    g_pending_zoom_percent = 0;
    fflush(stdout);
}

static void apply_pending_window_reset_locked(void)
{
    if(g_display == NULL || !g_pending_reset_window_to_target_size || g_target_hor_res <= 0 || g_target_ver_res <= 0) {
        return;
    }

    SDL_Window *window = lv_sdl_window_get_window(g_display);
    if(window == NULL) {
        return;
    }

    printf("Resetting simulator window to original size %dx%d.\n", g_target_hor_res, g_target_ver_res);
    SDL_SetWindowSize(window, g_target_hor_res, g_target_ver_res);
    g_pending_reset_window_to_target_size = false;
    fflush(stdout);
}

static void apply_pending_window_visibility_locked(void)
{
    if(g_display == NULL || !g_pending_show_window || g_window_visible) {
        return;
    }

    SDL_Window *window = lv_sdl_window_get_window(g_display);
    if(window == NULL) {
        return;
    }

    SDL_ShowWindow(window);
    g_pending_show_window = false;
    g_window_visible = true;
    fflush(stdout);
}

static void fit_display_zoom_to_window_locked(void)
{
    if(g_display == NULL || g_target_hor_res <= 0 || g_target_ver_res <= 0) {
        return;
    }

    SDL_Window *window = lv_sdl_window_get_window(g_display);
    if(window == NULL) {
        return;
    }

    int window_width = 0;
    int window_height = 0;
    SDL_GetWindowSize(window, &window_width, &window_height);
    if(window_width <= 0 || window_height <= 0) {
        return;
    }

    const float zoom_x = (float)window_width / (float)g_target_hor_res;
    const float zoom_y = (float)window_height / (float)g_target_ver_res;
    float desired_zoom = zoom_x < zoom_y ? zoom_x : zoom_y;
    if(desired_zoom < 0.10f) {
        desired_zoom = 0.10f;
    }

    const float current_zoom = lv_sdl_window_get_zoom(g_display);
    const int current_hor_res = lv_display_get_horizontal_resolution(g_display);
    const int current_ver_res = lv_display_get_vertical_resolution(g_display);
    const bool resolution_changed = current_hor_res != g_target_hor_res || current_ver_res != g_target_ver_res;
    const bool zoom_changed = fabsf(current_zoom - desired_zoom) > 0.01f;
    if(!resolution_changed && !zoom_changed) {
        return;
    }

    printf(
        "Fitting simulator content to window %dx%d. target=%dx%d zoom=%.2f\n",
        window_width,
        window_height,
        g_target_hor_res,
        g_target_ver_res,
        desired_zoom);
    lv_display_set_resolution(g_display, g_target_hor_res, g_target_ver_res);
    lv_sdl_window_set_zoom(g_display, desired_zoom);
    fflush(stdout);
}

static void apply_initial_window_position_locked(void)
{
    if(g_display == NULL) {
        return;
    }

    const char *x_value = getenv("MCU_UI_PREVIEW_START_X");
    const char *y_value = getenv("MCU_UI_PREVIEW_START_Y");
    if(x_value == NULL || y_value == NULL) {
        return;
    }

    char *end_ptr = NULL;
    long x = strtol(x_value, &end_ptr, 10);
    if(end_ptr == x_value || *end_ptr != '\0') {
        return;
    }

    end_ptr = NULL;
    long y = strtol(y_value, &end_ptr, 10);
    if(end_ptr == y_value || *end_ptr != '\0') {
        return;
    }

    SDL_Window *window = lv_sdl_window_get_window(g_display);
    if(window == NULL) {
        return;
    }

    SDL_SetWindowPosition(window, (int)x, (int)y);
}

static void apply_initial_window_zoom_locked(void)
{
    if(g_display == NULL) {
        return;
    }

    const char *zoom_value = getenv("MCU_UI_PREVIEW_ZOOM_PERCENT");
    if(zoom_value == NULL) {
        return;
    }

    char *end_ptr = NULL;
    long zoom_percent = strtol(zoom_value, &end_ptr, 10);
    if(end_ptr == zoom_value || *end_ptr != '\0' || zoom_percent <= 0) {
        return;
    }

    lv_sdl_window_set_zoom(g_display, (float)zoom_percent / 100.0f);
}
#endif

bool lvgl_runtime_prepare_platform(lvgl_simulator_runtime_t *runtime)
{
    if(runtime == NULL) {
        return false;
    }

#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    printf("Preparing SDL2 window and platform glue.\n");

    g_shutdown_requested = false;
    g_default_group = NULL;
    g_mouse_indev = NULL;
    g_mousewheel_indev = NULL;
    g_keyboard_indev = NULL;
    g_pending_hor_res = 0;
    g_pending_ver_res = 0;
    g_pending_zoom_percent = 0;
    g_target_hor_res = SIM_HOR_RES;
    g_target_ver_res = SIM_VER_RES;
    g_pending_reset_window_to_target_size = false;
    g_pending_show_window = false;
    g_window_visible = true;
    lv_init();
    lv_log_register_print_cb(simulator_lvgl_log_cb);

    if(access("assets/lvgl_logo.png", R_OK) == 0) {
        printf("Verified runtime image asset: assets/lvgl_logo.png\n");
    }
    else {
        printf("Runtime image asset is not readable: assets/lvgl_logo.png\n");
    }
    fflush(stdout);

    g_display = lv_sdl_window_create(SIM_HOR_RES, SIM_VER_RES);
    if(g_display == NULL) {
        fprintf(stderr, "lv_sdl_window_create failed.\n");
        return false;
    }

    lv_sdl_window_set_title(g_display, "MCU UI Studio for LVGL");
    lv_sdl_window_set_resizeable(g_display, true);
    apply_initial_window_zoom_locked();
    apply_initial_window_position_locked();
    lvgl_runtime_hide_window();

    g_default_group = lv_group_create();
    if(g_default_group == NULL) {
        fprintf(stderr, "lv_group_create failed.\n");
        return false;
    }

    lv_group_set_default(g_default_group);

    g_mouse_indev = lv_sdl_mouse_create();
    g_mousewheel_indev = lv_sdl_mousewheel_create();
    g_keyboard_indev = lv_sdl_keyboard_create();

    if(g_mouse_indev != NULL) {
        lv_indev_set_group(g_mouse_indev, g_default_group);
    }

    if(g_mousewheel_indev != NULL) {
        lv_indev_set_group(g_mousewheel_indev, g_default_group);
    }

    if(g_keyboard_indev != NULL) {
        lv_indev_set_group(g_keyboard_indev, g_default_group);
    }

    runtime->platform_ready = true;
    fflush(stdout);
    return true;
#else
    printf("Platform preparation skipped because SDL2/LVGL are not fully configured.\n");
    runtime->platform_ready = false;
    fflush(stdout);
    return true;
#endif
}

bool lvgl_runtime_initialize_lvgl(lvgl_simulator_runtime_t *runtime)
{
    if(runtime == NULL) {
        return false;
    }

#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    printf("Initializing LVGL core, display driver and input backend.\n");
#else
    printf("LVGL initialization stays in placeholder mode.\n");
#endif

    fflush(stdout);
    return true;
}

int lvgl_runtime_run_platform_loop(lvgl_simulator_runtime_t *runtime)
{
    if(runtime == NULL) {
        return 1;
    }

#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    if(!runtime->platform_ready) {
        fprintf(stderr, "Platform loop requested without a ready platform.\n");
        return 1;
    }

    while(!g_shutdown_requested) {
        uint32_t sleep_ms;

        pthread_mutex_lock(&g_lvgl_mutex);
        apply_pending_display_size_locked();
        apply_pending_display_zoom_locked();
        apply_pending_window_reset_locked();
        apply_pending_window_visibility_locked();
        fit_display_zoom_to_window_locked();
        sleep_ms = lv_timer_handler();
        pthread_mutex_unlock(&g_lvgl_mutex);

        if(sleep_ms > 20U) {
            sleep_ms = 20U;
        }

        usleep((useconds_t)sleep_ms * 1000U);
    }

    return 0;
#else
    (void)runtime;
    while(!g_shutdown_requested) {
        usleep(20000U);
    }

    return 0;
#endif
}

void lvgl_runtime_request_platform_shutdown(void)
{
#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    g_shutdown_requested = true;
#else
    g_shutdown_requested = true;
#endif
}

void lvgl_runtime_request_display_size(int width, int height)
{
#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    if(width <= 0 || height <= 0) {
        return;
    }

    g_target_hor_res = width;
    g_target_ver_res = height;
    g_pending_hor_res = width;
    g_pending_ver_res = height;
#else
    (void)width;
    (void)height;
#endif
}

void lvgl_runtime_request_zoom_percent(int zoom_percent)
{
#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    if(zoom_percent <= 0) {
        return;
    }

    pthread_mutex_lock(&g_lvgl_mutex);
    g_pending_zoom_percent = zoom_percent;
    pthread_mutex_unlock(&g_lvgl_mutex);
#else
    (void)zoom_percent;
#endif
}

void lvgl_runtime_request_reset_window_to_target_size(void)
{
#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    pthread_mutex_lock(&g_lvgl_mutex);
    g_pending_reset_window_to_target_size = true;
    pthread_mutex_unlock(&g_lvgl_mutex);
#endif
}

void lvgl_runtime_hide_window(void)
{
#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    if(g_display == NULL) {
        return;
    }

    SDL_Window *window = lv_sdl_window_get_window(g_display);
    if(window == NULL) {
        return;
    }

    SDL_HideWindow(window);
    g_window_visible = false;
#endif
}

void lvgl_runtime_show_window(void)
{
#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    if(g_display == NULL || g_window_visible) {
        return;
    }

    g_pending_show_window = true;
#endif
}

bool lvgl_runtime_lock(void)
{
#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    return pthread_mutex_lock(&g_lvgl_mutex) == 0;
#else
    return true;
#endif
}

void lvgl_runtime_unlock(void)
{
#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    pthread_mutex_unlock(&g_lvgl_mutex);
#endif
}

void lvgl_runtime_shutdown_platform(lvgl_simulator_runtime_t *runtime)
{
    if(runtime == NULL) {
        return;
    }

#if LVGL_SIMULATOR_HAS_SDL2 && LVGL_SIMULATOR_HAS_LVGL
    printf("Shutting down LVGL and SDL2 platform resources.\n");
    g_shutdown_requested = true;
    if(g_keyboard_indev != NULL) {
        lv_indev_set_group(g_keyboard_indev, NULL);
        g_keyboard_indev = NULL;
    }

    if(g_mousewheel_indev != NULL) {
        lv_indev_set_group(g_mousewheel_indev, NULL);
        g_mousewheel_indev = NULL;
    }

    if(g_mouse_indev != NULL) {
        lv_indev_set_group(g_mouse_indev, NULL);
        g_mouse_indev = NULL;
    }

    if(g_default_group != NULL) {
        lv_group_set_default(NULL);
        lv_group_delete(g_default_group);
        g_default_group = NULL;
    }
#else
    printf("Platform shutdown in placeholder mode.\n");
#endif

    runtime->platform_ready = false;
    fflush(stdout);
}
